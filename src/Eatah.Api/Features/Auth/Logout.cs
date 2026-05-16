using Eatah.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Eatah.Api.Features.Auth;

public static class Logout
{
    public static async Task<IResult> Handle(SignInManager<EatahUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return Results.NoContent();
    }
}
