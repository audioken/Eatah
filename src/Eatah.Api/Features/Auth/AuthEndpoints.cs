using Eatah.Api.Features.Auth.Email;
using Eatah.Infrastructure.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Eatah.Api.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var publicGroup = app.MapGroup("/api/auth")
            .WithTags("Auth")
            .AllowAnonymous();

        publicGroup.MapPost("/register", RegisterEmail.Handle).WithName(nameof(RegisterEmail));
        publicGroup.MapPost("/confirm", ConfirmEmailAndSetCredentials.Handle).WithName(nameof(ConfirmEmailAndSetCredentials));
        publicGroup.MapPost("/login", Login.Handle).WithName(nameof(Login));
        publicGroup.MapPost("/password-reset/request", RequestPasswordReset.Handle).WithName(nameof(RequestPasswordReset));
        publicGroup.MapPost("/password-reset", ResetPassword.Handle).WithName(nameof(ResetPassword));
        publicGroup.MapGet("/check-displayname", CheckDisplayName.Handle).WithName(nameof(CheckDisplayName));

        var authedGroup = app.MapGroup("/api/auth")
            .WithTags("Auth")
            .RequireAuthorization();

        authedGroup.MapPost("/logout", Logout.Handle).WithName(nameof(Logout));
        authedGroup.MapPost("/password-change", ChangePassword.Handle).WithName(nameof(ChangePassword));
        authedGroup.MapGet("/me", Me.Handle).WithName(nameof(Me));

        return app;
    }
}

public static class AuthServiceExtensions
{
    /// <summary>
    /// Registers ASP.NET Identity (cookie-based), the auth feature's validators, settings,
    /// and an <see cref="IEmailSender"/>. Falls back to <see cref="ConsoleEmailSender"/> when
    /// the <c>Smtp:Host</c> setting is empty (development convenience).
    /// </summary>
    public static IServiceCollection AddAuthFeature(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        services.Configure<BrevoSettings>(configuration.GetSection(BrevoSettings.SectionName));
        services.Configure<AuthSettings>(configuration.GetSection(AuthSettings.SectionName));

        // Identity options
        services.AddIdentity<Eatah.Infrastructure.Identity.EatahUser, IdentityRole<Guid>>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = true;

            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = AuthValidationRules.PasswordMinLength;
            options.Password.RequiredUniqueChars = 1;

            options.Lockout.MaxFailedAccessAttempts = 10;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddEntityFrameworkStores<Eatah.Infrastructure.Persistence.EatahDbContext>()
        .AddDefaultTokenProviders();

        // Multi-scheme: MAUI uses cookies; WebClient uses JWT Bearer.
        var authSettings = configuration.GetSection(AuthSettings.SectionName).Get<AuthSettings>() ?? new();
        var keyBytes = Encoding.UTF8.GetBytes(
            string.IsNullOrWhiteSpace(authSettings.JwtSecret)
                ? "dev-only-secret-replace-in-production-minimum-32-chars"
                : authSettings.JwtSecret);

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, opts =>
            {
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = authSettings.JwtIssuer,
                    ValidAudience = authSettings.JwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
                };
                opts.Events = new JwtBearerEvents
                {
                    OnChallenge = ctx =>
                    {
                        ctx.HandleResponse();
                        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(opts =>
        {
            opts.DefaultPolicy = new AuthorizationPolicyBuilder(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build();
        });

        services.ConfigureApplicationCookie(opts =>
        {
            opts.Cookie.Name = "eatah.auth";
            opts.Cookie.HttpOnly = true;
            // SameSite=None + Secure=Always is required so the cookie is sent on
            // cross-origin requests from the Blazor WASM client (audioken.github.io → eatah.onrender.com).
            opts.Cookie.SameSite = SameSiteMode.None;
            opts.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            opts.ExpireTimeSpan = TimeSpan.FromDays(30);
            opts.SlidingExpiration = true;
            opts.Events.OnRedirectToLogin = ctx =>
            {
                // API: respond with 401 instead of redirecting to a login page.
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            opts.Events.OnRedirectToAccessDenied = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        // Email sender — Brevo HTTP API if configured (bypasses SMTP port blocking on cloud
        // platforms), else real SMTP if host is set, else dev console fallback.
        services.AddHttpClient();
        services.AddSingleton<IEmailSender>(sp =>
        {
            var brevo = sp.GetRequiredService<IOptions<BrevoSettings>>().Value;
            if (brevo.IsConfigured)
            {
                return new BrevoHttpEmailSender(
                    sp.GetRequiredService<IOptions<BrevoSettings>>(),
                    sp.GetRequiredService<IOptions<SmtpSettings>>(),
                    sp.GetRequiredService<IHttpClientFactory>(),
                    sp.GetRequiredService<ILogger<BrevoHttpEmailSender>>());
            }

            var smtp = sp.GetRequiredService<IOptions<SmtpSettings>>().Value;
            if (smtp.IsConfigured)
            {
                return new SmtpEmailSender(sp.GetRequiredService<IOptions<SmtpSettings>>(),
                    sp.GetRequiredService<ILogger<SmtpEmailSender>>());
            }
            return new ConsoleEmailSender(sp.GetRequiredService<ILogger<ConsoleEmailSender>>());
        });

        services.AddScoped<IValidator<RegisterEmailRequest>, RegisterEmailValidator>();
        services.AddScoped<IValidator<ConfirmEmailRequest>, ConfirmEmailValidator>();
        services.AddScoped<IValidator<LoginRequest>, LoginValidator>();
        services.AddScoped<IValidator<RequestPasswordResetRequest>, RequestPasswordResetValidator>();
        services.AddScoped<IValidator<ResetPasswordRequest>, ResetPasswordValidator>();
        services.AddScoped<IValidator<ChangePasswordRequest>, ChangePasswordValidator>();

        return services;
    }
}

/// <summary>
/// JWT generation helper shared by login, confirm and reset-password endpoints.
/// </summary>
internal static class JwtTokenHelper
{
    internal static string GenerateToken(EatahUser user, AuthSettings settings)
    {
        var secret = string.IsNullOrWhiteSpace(settings.JwtSecret)
            ? "dev-only-secret-replace-in-production-minimum-32-chars"
            : settings.JwtSecret;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Name, user.DisplayName)
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(settings.JwtExpiryDays),
            Issuer = settings.JwtIssuer,
            Audience = settings.JwtAudience,
            SigningCredentials = creds
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }
}
