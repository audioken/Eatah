using Eatah.Api.Common;
using Eatah.Api.Features.Auth.Email;
using Eatah.Infrastructure.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Eatah.Api.Features.Auth;

public static class RegisterEmail
{
    public static async Task<IResult> Handle(
        RegisterEmailRequest request,
        IValidator<RegisterEmailRequest> validator,
        UserManager<EatahUser> userManager,
        IEmailSender emailSender,
        IOptions<AuthSettings> authSettings,
        ILogger<RegisterEmailRequest> logger,
        CancellationToken ct)
    {
        var validationError = await validator.ValidateRequestAsync(request, ct);
        if (validationError is not null)
        {
            return Result<RegistrationResponse>.Failure(validationError).ToHttpResult();
        }

        var email = request.Email.Trim();
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (existing.EmailConfirmed)
            {
                return Result<RegistrationResponse>
                    .Failure(Error.Conflict(ErrorCodes.AuthEmailTaken, "An account with this email already exists."))
                    .ToHttpResult();
            }
            // Unconfirmed: re-issue confirmation email instead of creating a duplicate.
            await SendConfirmationEmailAsync(existing, userManager, emailSender, authSettings.Value, ct);
            return Results.Ok(new RegistrationResponse(existing.Id, existing.Email!, true));
        }

        var user = new EatahUser
        {
            Id = Guid.NewGuid(),
            UserName = email, // temporary; replaced with DisplayName at confirm step
            Email = email,
            EmailConfirmed = false,
            DisplayName = $"_pending_{Guid.NewGuid():N}" // unique placeholder; overwritten at confirm step
        };

        var create = await userManager.CreateAsync(user);
        if (!create.Succeeded)
        {
            logger.LogWarning("User creation failed for {Email}: {Errors}", email,
                string.Join("; ", create.Errors.Select(e => e.Description)));
            return Result<RegistrationResponse>
                .Failure(Error.BadRequest(ErrorCodes.AuthPasswordInvalid, create.Errors.First().Description))
                .ToHttpResult();
        }

        await SendConfirmationEmailAsync(user, userManager, emailSender, authSettings.Value, ct);

        return Results.Ok(new RegistrationResponse(user.Id, user.Email!, true));
    }

    private static async Task SendConfirmationEmailAsync(
        EatahUser user,
        UserManager<EatahUser> userManager,
        IEmailSender emailSender,
        AuthSettings authSettings,
        CancellationToken ct)
    {
        var rawToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encoded = TokenEncoding.Encode(rawToken);
        var url = $"{authSettings.ClientBaseUrl.TrimEnd('/')}/auth/confirm?userId={user.Id}&token={encoded}";
        var content = EmailTemplates.ConfirmEmail(url);
        await emailSender.SendAsync(user.Email!, content.Subject, content.HtmlBody, content.TextBody, ct);
    }
}
