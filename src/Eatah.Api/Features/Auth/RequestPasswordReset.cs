using Eatah.Api.Common;
using Eatah.Api.Features.Auth.Email;
using Eatah.Infrastructure.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Eatah.Api.Features.Auth;

public static class RequestPasswordReset
{
    public static async Task<IResult> Handle(
        RequestPasswordResetRequest request,
        IValidator<RequestPasswordResetRequest> validator,
        UserManager<EatahUser> userManager,
        IEmailSender emailSender,
        IOptions<AuthSettings> authSettings,
        CancellationToken ct)
    {
        // Validate input format but ALWAYS return 200 so we don't reveal whether the address exists.
        var validationError = await validator.ValidateRequestAsync(request, ct);
        if (validationError is not null)
        {
            return Result.Failure(validationError).ToNoContentResult();
        }

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is not null && user.EmailConfirmed)
        {
            var rawToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var encoded = TokenEncoding.Encode(rawToken);
            var url = $"{authSettings.Value.ClientBaseUrl.TrimEnd('/')}/auth/reset-password?userId={user.Id}&token={encoded}";
            var content = EmailTemplates.PasswordReset(url);
            await emailSender.SendAsync(user.Email!, content.Subject, content.HtmlBody, content.TextBody, ct);
        }

        return Results.NoContent();
    }
}
