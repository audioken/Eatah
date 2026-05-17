using System.Security.Claims;
using Eatah.Api.Common;
using Eatah.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Eatah.Api.Features.Auth;

public static class UpdateProfile
{
    public static async Task<IResult> Handle(
        UpdateProfileRequest request,
        UserManager<EatahUser> userManager,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null)
            return Error.Unauthorized(ErrorCodes.AuthNotAuthenticated, "Not authenticated.").ToHttpResult();

        var displayName = request.DisplayName?.Trim();
        var email = request.Email?.Trim();

        if (!string.IsNullOrWhiteSpace(displayName) &&
            !string.Equals(displayName, user.DisplayName, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await userManager.FindByNameAsync(displayName);
            if (existing is not null && existing.Id != user.Id)
                return Error.Conflict(ErrorCodes.AuthDisplayNameTaken, "Display name is already taken.").ToHttpResult();

            user.DisplayName = displayName;
            user.UserName = displayName;
        }

        if (!string.IsNullOrWhiteSpace(email) &&
            !string.Equals(email, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await userManager.FindByEmailAsync(email);
            if (existing is not null && existing.Id != user.Id)
                return Error.Conflict(ErrorCodes.AuthEmailTaken, "Email is already taken.").ToHttpResult();

            user.Email = email;
            user.NormalizedEmail = email.ToUpperInvariant();
        }

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return Error.Unexpected(ErrorCodes.Unexpected, "Could not update profile.").ToHttpResult();

        return Results.Ok(new UserResponse(user.Id, user.Email!, user.DisplayName));
    }
}
