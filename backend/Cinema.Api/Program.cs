using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Cinema.Application.Auth;
using Cinema.Infrastructure;
using Cinema.Infrastructure.Identity;
using Cinema.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// серіалізуємо enum-и як рядки, щоб Angular міг читати "Paid" замість 2
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        opt.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCinemaInfrastructure(builder.Configuration);

// реєструємо валідатори з Application шару
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

// rate limiting для ендпоінту login
var rateLimitSection = builder.Configuration.GetSection("RateLimit");
builder.Services.AddRateLimiter(opt =>
{
    opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opt.AddFixedWindowLimiter("login", limiterOpt =>
    {
        limiterOpt.PermitLimit = rateLimitSection.GetValue("LoginPermitLimit", 5);
        limiterOpt.Window = TimeSpan.FromSeconds(rateLimitSection.GetValue("LoginWindowSeconds", 60));
        limiterOpt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOpt.QueueLimit = 0;
    });
});

var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? [];

var singleAllowedOrigin = builder.Configuration["ALLOWED_ORIGIN"];
if (!string.IsNullOrWhiteSpace(singleAllowedOrigin))
{
    allowedOrigins = [.. allowedOrigins, singleAllowedOrigin];
}

allowedOrigins = allowedOrigins
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(policy =>
        policy.SetIsOriginAllowed(origin =>
            allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase)
            || Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            && (uri.Host.EndsWith(".ngrok-free.app", StringComparison.OrdinalIgnoreCase)
                || uri.Host.EndsWith(".ngrok-free.dev", StringComparison.OrdinalIgnoreCase)
                || uri.Host is "localhost" or "127.0.0.1"))
              .AllowAnyHeader()
              .AllowAnyMethod()
              .WithExposedHeaders("Content-Disposition")));

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // застосовуємо міграції та сідуємо дані при старті лише в Development
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    await db.Database.MigrateAsync();
    await CinemaDbSeeder.SeedAsync(db, roleManager, userManager);
}

 app.UseCors();
 
 // Security headers
 app.Use(async (ctx, next) =>
 {
     ctx.Response.Headers["Content-Security-Policy"] =
         "default-src 'self'; " +
         "script-src 'self' 'unsafe-inline' https://js.stripe.com https://www.paypalobjects.com https://pay.google.com; " +
         "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
         "font-src 'self' https://fonts.gstatic.com; " +
         "img-src 'self' data: https:; " +
         "connect-src 'self' https://api.stripe.com https://api.paypal.com; " +
         "frame-src https://www.paypal.com https://www.sandbox.paypal.com;";
     ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
     ctx.Response.Headers["X-Frame-Options"] = "DENY";
     ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
     await next();
 });
 
 app.UseRateLimiter();
 app.UseAuthentication();
 app.UseAuthorization();
 app.MapControllers();
 app.MapHealthChecks("/health");

await app.RunAsync();

// дозволяємо інтеграційним тестам посилатися на цей assembly
public partial class Program { }
