using Eatah.Api.Features.Auth.Email;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

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

        services.ConfigureApplicationCookie(opts =>
        {
            opts.Cookie.Name = "eatah.auth";
            opts.Cookie.HttpOnly = true;
            opts.Cookie.SameSite = SameSiteMode.Lax;
            opts.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
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

        // Email sender — real SMTP if configured, else dev console.
        services.AddSingleton<IEmailSender>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<SmtpSettings>>().Value;
            if (settings.IsConfigured)
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
