using System.Security.Claims;
using Eatah.Api.Common;
using Eatah.Infrastructure.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Eatah.Api.Features.Auth;

public static class ChangePassword
{
    public static async Task<IResult> Handle(
        ChangePasswordRequest request,
        IValidator<ChangePasswordRequest> validator,
        UserManager<EatahUser> userManager,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        var validationError = await validator.ValidateRequestAsync(request, ct);
        if (validationError is not null)
        {
            return Result.Failure(validationError).ToNoContentResult();
        }

        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return Result
                .Failure(Error.BadRequest(ErrorCodes.AuthNotAuthenticated, "Not authenticated."))
                .ToNoContentResult();
        }

        var change = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!change.Succeeded)
        {
            var first = change.Errors.First();
            var code = first.Code.Contains("Password", StringComparison.OrdinalIgnoreCase)
                ? ErrorCodes.AuthPasswordInvalid
                : ErrorCodes.AuthInvalidCredentials;
            return Result
                .Failure(Error.BadRequest(code, first.Description))
                .ToNoContentResult();
        }

        return Results.NoContent();
    }
}
