using System.Text;
using Cinema.Application.Account;
using Cinema.Application.Auth;
using Cinema.Application.Tmdb;
using Cinema.Application.Cashier;
using Cinema.Application.Reviews;
using Cinema.Application.PromoCodes;
using Cinema.Application.Cinemas;
using Cinema.Application.Halls;
using Cinema.Application.Users;
using Cinema.Application.Email;
using Cinema.Application.Genres;
using Cinema.Application.Loyalty;
using Cinema.Application.Movies;
using Cinema.Application.Payments;
using Cinema.Application.QrCode;
using Cinema.Application.Showtimes;
using Cinema.Application.Reports;
using Cinema.Application.Tickets;
using Cinema.Infrastructure.Account;
using Cinema.Infrastructure.Admin;
using Cinema.Infrastructure.Tmdb;
using Cinema.Infrastructure.Cashier;
using Cinema.Infrastructure.Reports;
using Cinema.Infrastructure.Reviews;
using Cinema.Infrastructure.PromoCodes;
using Cinema.Infrastructure.Loyalty;
using Cinema.Infrastructure.Email;
using Cinema.Infrastructure.Identity;
using Cinema.Infrastructure.Payments;
using Cinema.Infrastructure.Persistence;
using Cinema.Infrastructure.Queries;
using Cinema.Infrastructure.Tickets;
using Cinema.Infrastructure.Workers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;
using Stripe;
using System.Net;

namespace Cinema.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCinemaInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        services.AddDbContext<CinemaDbContext>(opt =>
            opt.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

        services.AddIdentity<ApplicationUser, IdentityRole>(opt =>
            {
                opt.Password.RequireDigit = true;
                opt.Password.RequiredLength = 6;
                opt.Password.RequireNonAlphanumeric = false;
                opt.Password.RequireUppercase = false;
            })
            .AddEntityFrameworkStores<CinemaDbContext>()
            .AddDefaultTokenProviders();

        var jwtSection = configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSection["Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured."));

        services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(opt =>
            {
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidAudience = jwtSection["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero,
                    RoleClaimType = "role",
                };
                opt.MapInboundClaims = false;
            });

        services.AddScoped<ITokenService, Identity.TokenService>();

        services.AddScoped<IMovieQueryService, MovieQueryService>();
        services.AddScoped<IShowtimeQueryService, ShowtimeQueryService>();
        services.AddScoped<ICinemaQueryService, CinemaQueryService>();
        services.AddScoped<ICinemaAdminService, CinemaAdminService>();
        services.AddScoped<IMovieAdminService, MovieAdminService>();
        services.AddScoped<IHallAdminService, HallAdminService>();
        services.AddScoped<IShowtimeAdminService, ShowtimeAdminService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddSingleton<PdfReportExporter>();
        services.AddSingleton<ExcelReportExporter>();
        services.AddScoped<IGenreQueryService, GenreQueryService>();

        services.AddScoped<ITicketService, TicketService>();
        services.AddHostedService<ShowtimeReminderWorker>();
        services.AddHostedService<AbandonedPaymentCleanupWorker>();

        services.AddScoped<IAccountService, Infrastructure.Account.AccountService>();
        services.AddScoped<ILoyaltyService, LoyaltyService>();
        services.AddScoped<IReviewService, Reviews.ReviewService>();
        services.AddScoped<IPromoCodeService, PromoCodeService>();
        services.AddScoped<ICashierService, Infrastructure.Cashier.CashierService>();
        services.AddScoped<Cinema.Application.Users.IStaffUserService, StaffUserService>();

        // --- QR + Email ---
        services.AddSingleton<IQrCodeGenerator, Infrastructure.QrCode.QrCodeGenerator>();

        var emailSection = configuration.GetSection("Email");
        services.Configure<EmailOptions>(emailSection);
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        // --- Payments ---
        var stripeOpts  = configuration.GetSection("Stripe");
        var paypalOpts  = configuration.GetSection("PayPal");

        services.Configure<StripeOptions>(stripeOpts);
        services.Configure<PayPalOptions>(paypalOpts);

        // Stripe client реєструється як singleton — він thread-safe
        services.AddSingleton<IStripeClient>(_ =>
            new StripeClient(stripeOpts["SecretKey"] ?? "sk_test_placeholder"));

        // ... existing code
        services.AddSingleton<IStripeWebhookVerifier, StripeWebhookVerifier>();
        services.AddScoped<IPayPalWebhookVerifier, PayPalWebhookVerifier>();

        // Register memory cache for exchange rate caching
        services.AddMemoryCache();

        // PayPal використовує окремий HttpClient (може мати timeout, base address тощо)
        services.AddHttpClient<PayPalProvider>(client =>
        {
            var baseUrl = paypalOpts["BaseUrl"] ?? "https://api-m.sandbox.paypal.com";
            client.BaseAddress = new Uri(baseUrl);
        });

        services.AddScoped<IPaymentProvider, StripeProvider>();
        services.AddScoped<IPaymentProvider>(sp => sp.GetRequiredService<PayPalProvider>());

        services.AddScoped<IPaymentService, PaymentService>();

        // Exchange rate service
        services.AddScoped<IExchangeRateService, ExchangeRateService>();

        // --- TMDB ---
        var tmdbSection = configuration.GetSection("Tmdb");
        services.Configure<TmdbOptions>(tmdbSection);
        services.AddHttpClient<ITmdbService, TmdbService>(client =>
        {
            var baseUrl = tmdbSection["BaseUrl"] ?? "https://api.themoviedb.org/3";
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        })
        .AddPolicyHandler(HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));

        return services;
    }
}
