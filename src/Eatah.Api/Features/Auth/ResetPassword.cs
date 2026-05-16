using Eatah.Api.Common;
using Eatah.Infrastructure.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace Eatah.Api.Features.Auth;

public static class ResetPassword
{
    public static async Task<IResult> Handle(
        ResetPasswordRequest request,
        IValidator<ResetPasswordRequest> validator,
        UserManager<EatahUser> userManager,
        SignInManager<EatahUser> signInManager,
        CancellationToken ct)
    {
        var validationError = await validator.ValidateRequestAsync(request, ct);
        if (validationError is not null)
        {
            return Result<UserResponse>.Failure(validationError).ToHttpResult();
        }

        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return Result<UserResponse>
                .Failure(Error.BadRequest(ErrorCodes.AuthInvalidToken, "Reset token is invalid."))
                .ToHttpResult();
        }

        if (!TokenEncoding.TryDecode(request.Token, out var rawToken))
        {
            return Result<UserResponse>
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
            return Result<UserResponse>
                .Failure(Error.BadRequest(code, first.Description))
                .ToHttpResult();
        }

        await signInManager.SignInAsync(user, isPersistent: true);
        return Results.Ok(new UserResponse(user.Id, user.Email!, user.DisplayName));
    }
}
