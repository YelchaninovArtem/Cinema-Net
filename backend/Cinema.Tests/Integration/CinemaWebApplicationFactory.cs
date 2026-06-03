using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Cinema.Application.Auth;
using Cinema.Application.Email;
using Cinema.Domain.Entities;
using Cinema.Infrastructure.Identity;
using Cinema.Infrastructure.Payments;
using Cinema.Infrastructure.Persistence;
using Cinema.Infrastructure.Workers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stripe;
using Testcontainers.MsSql;

namespace Cinema.Tests.Integration;

/// <summary>Повертає мінімальну відповідь PaymentIntent щоб Stripe SDK не робив реальних HTTP-запитів.</summary>
public sealed class FakeStripeHttpHandler : HttpMessageHandler
{
    private static int _counter;
    private readonly ConcurrentDictionary<string, long> _intentAmounts = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var path = request.RequestUri?.AbsolutePath ?? "";
        if (path.EndsWith("/v1/payment_methods", StringComparison.OrdinalIgnoreCase))
        {
            var paymentMethodJson = """
                {
                  "id": "pm_test_google_pay",
                  "object": "payment_method",
                  "type": "card",
                  "card": {
                    "brand": "visa",
                    "last4": "4242",
                    "exp_month": 12,
                    "exp_year": 2030
                  },
                  "livemode": false
                }
                """;
            return JsonResponse(paymentMethodJson);
        }

        if (request.Method == HttpMethod.Get && path.Contains("/v1/payment_intents/", StringComparison.OrdinalIgnoreCase))
        {
            var existingId = path.Split('/').Last();
            var existingAmount = _intentAmounts.GetValueOrDefault(existingId, 10000);
            return JsonResponse(PaymentIntentJson(existingId, existingAmount, "succeeded"));
        }

        var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct);
        var id = $"pi_test_{Interlocked.Increment(ref _counter)}";
        var amountMatch = Regex.Match(body, @"(?:^|&)amount=(\d+)");
        var amount = amountMatch.Success ? long.Parse(amountMatch.Groups[1].Value) : 10000;
        _intentAmounts[id] = amount;
        var status = body.Contains("confirm=true", StringComparison.OrdinalIgnoreCase)
            ? "succeeded"
            : "requires_payment_method";
        return JsonResponse(PaymentIntentJson(id, amount, status));
    }

    private static string PaymentIntentJson(string id, long amount, string status) => $$"""
            {
              "id": "{{id}}",
              "object": "payment_intent",
              "client_secret": "{{id}}_secret",
              "amount": {{amount}},
              "currency": "uah",
              "status": "{{status}}",
              "livemode": false,
              "created": 1700000000,
              "payment_method_types": ["card"],
              "metadata": {}
            }
            """;

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }
}

public sealed class CapturingEmailSender : IEmailSender
{
    private readonly List<(string To, string Subject, int Attachments, IReadOnlyList<string> FileNames, IReadOnlyList<string> MimeTypes)> _sent = [];
    public IReadOnlyList<(string To, string Subject, int Attachments, IReadOnlyList<string> FileNames, IReadOnlyList<string> MimeTypes)> Sent => _sent.AsReadOnly();

    public Task SendAsync(string to, string subject, string htmlBody,
        IEnumerable<EmailAttachment>? attachments = null, CancellationToken ct = default)
    {
        var attachmentList = attachments?.ToList() ?? [];
        _sent.Add((
            to,
            subject,
            attachmentList.Count,
            attachmentList.Select(a => a.FileName).ToList(),
            attachmentList.Select(a => a.MimeType).ToList()));
        return Task.CompletedTask;
    }
}

public sealed class CinemaWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public CapturingEmailSender EmailSender { get; } = new();
    private readonly MsSqlContainer _mssql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public async Task LoginAdminAsync(HttpClient client)
    {
        var request = new LoginRequest(Email: "admin@cinema.local", Password: "Admin_123!");
        
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", request);
        
        if (!loginResponse.IsSuccessStatusCode)
        {
            var errorContent = await loginResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Login failed: {loginResponse.StatusCode} - {errorContent}");
        }
        
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        
        if (auth is null)
            throw new InvalidOperationException("Failed to deserialize AuthResponse");
        
        client.DefaultRequestHeaders.Authorization = new("Bearer", auth.AccessToken);
    }

    public async Task LoginClientAsync(HttpClient client)
    {
        var request = new LoginRequest(Email: "client@cinema.local", Password: "Client_123!");
        
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", request);
        
        if (!loginResponse.IsSuccessStatusCode)
        {
            var errorContent = await loginResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Login failed: {loginResponse.StatusCode} - {errorContent}");
        }
        
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        
        if (auth is null)
            throw new InvalidOperationException("Failed to deserialize AuthResponse");
        
        client.DefaultRequestHeaders.Authorization = new("Bearer", auth.AccessToken);
    }

    public async Task InitializeAsync()
    {
        await _mssql.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Admin", "Cashier", "Client" })
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await EnsureUser(userManager, "admin@cinema.local", "Admin_123!", "Admin", "Admin", "Admin");
        await EnsureUser(userManager, "cashier@cinema.local", "Cashier_123!", "Cashier", "Cashier", "Cashier");
        await EnsureUser(userManager, "client@cinema.local", "Client_123!", "Client", "Client", "Client");

        await CinemaDbSeeder.SeedCatalogForTestsAsync(db);
    }

    private static async Task EnsureUser(UserManager<ApplicationUser> userManager, string email, string password, string role, string firstName, string lastName)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var user = new ApplicationUser { UserName = email, Email = email, FirstName = firstName, LastName = lastName, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Failed to seed user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        await userManager.AddToRoleAsync(user, role);
    }

    public new async Task DisposeAsync()
    {
        await _mssql.StopAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _mssql.GetConnectionString(),
            }));

        builder.ConfigureServices(services =>
        {
            // замінюємо DbContextOptions щоб EF Core використовував тестовий контейнер
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<CinemaDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<CinemaDbContext>(opt =>
                opt.UseSqlServer(_mssql.GetConnectionString()));

            // Прибираємо фонові workers щоб уникнути race condition у тестах;
            // AbandonedPaymentCleanupWorker тестується окремо.
            var workerTypes = new[] { typeof(AbandonedPaymentCleanupWorker), typeof(ShowtimeReminderWorker) };
            var workersToRemove = services
                .Where(d => d.ImplementationType is not null && workerTypes.Contains(d.ImplementationType))
                .ToList();
            foreach (var w in workersToRemove)
                services.Remove(w);

            // Замінюємо Stripe webhook verifier на версію без перевірки підпису (для тестів)
            var stripeVerifierDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IStripeWebhookVerifier));
            if (stripeVerifierDescriptor is not null)
                services.Remove(stripeVerifierDescriptor);
            services.AddSingleton<IStripeWebhookVerifier, NoVerificationStripeWebhookVerifier>();

            // Замінюємо PayPal webhook verifier на версію без перевірки (для тестів)
            var paypalVerifierDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IPayPalWebhookVerifier));
            if (paypalVerifierDescriptor is not null)
                services.Remove(paypalVerifierDescriptor);
            services.AddSingleton<IPayPalWebhookVerifier, NoVerificationPayPalWebhookVerifier>();

            // Stripe client замінюємо на stub що повертає фіктивний PaymentIntent без HTTP-запиту
            var stripeClientDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IStripeClient));
            if (stripeClientDescriptor is not null)
                services.Remove(stripeClientDescriptor);
            services.AddSingleton<IStripeClient>(_ =>
            {
                var handler = new FakeStripeHttpHandler();
                var httpClient = new SystemNetHttpClient(new HttpClient(handler));
                return new StripeClient("sk_test_placeholder", httpClient: httpClient);
            });

            // Замінюємо email sender на capturing stub
            var emailDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IEmailSender));
            if (emailDescriptor is not null)
                services.Remove(emailDescriptor);
            services.AddSingleton<IEmailSender>(EmailSender);
        });
    }
}
