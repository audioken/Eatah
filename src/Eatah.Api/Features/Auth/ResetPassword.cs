using Eatah.Api.Common;
using Eatah.Infrastructure.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Eatah.Api.Features.Auth;

public static class ResetPassword
{
    public static async Task<IResult> Handle(
        ResetPasswordRequest request,
        IValidator<ResetPasswordRequest> validator,
        UserManager<EatahUser> userManager,
        SignInManager<EatahUser> signInManager,
        IOptions<AuthSettings> authSettings,
        CancellationToken ct)
    {
        var validationError = await validator.ValidateRequestAsync(request, ct);
        if (validationError is not null)
        {
            return Result<AuthResponse>.Failure(validationError).ToHttpResult();
        }

        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return Result<AuthResponse>
                .Failure(Error.BadRequest(ErrorCodes.AuthInvalidToken, "Reset token is invalid."))
                .ToHttpResult();
        }

        if (!TokenEncoding.TryDecode(request.Token, out var rawToken))
        {
            return Result<AuthResponse>
                .Failure(Error.BadRequest(ErrorCodes.AuthInvalidToken, "Reset token is invalid."))
                .ToHttpResult();
        }

        var reset = await userManager.ResetPasswordAsync(user, rawToken, request.NewPassword);
        if (!reset.Succeeded)
        {
            var first = reset.Errors.First();
            var code = first.Code.Contains("Token", StringComparison.OrdinalIgnoreCase)
                ? ErrorCodes.AuthInvalidToken
                : ErrorCodes.AuthPasswordInvalid;
            return Result<AuthResponse>
                .Failure(Error.BadRequest(code, first.Description))
                .ToHttpResult();
        }

        await signInManager.SignInAsync(user, isPersistent: true);
        var token = JwtTokenHelper.GenerateToken(user, authSettings.Value);
        return Results.Ok(new AuthResponse(user.Id, user.Email!, user.DisplayName, token));
    }
}
