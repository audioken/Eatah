using System.Security.Claims;
using Eatah.Api.Common;
using Eatah.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Eatah.Api.Features.Auth;

public static class Me
{
    public static async Task<IResult> Handle(
        UserManager<EatahUser> userManager,
        ClaimsPrincipal principal)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return Result<UserResponse>
                .Failure(Error.BadRequest(ErrorCodes.AuthNotAuthenticated, "Not authenticated."))
                .ToHttpResult();
        }

        return Results.Ok(new UserResponse(user.Id, user.Email!, user.DisplayName));
    }
}
