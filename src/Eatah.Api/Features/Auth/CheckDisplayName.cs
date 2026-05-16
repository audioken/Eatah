using Eatah.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Eatah.Api.Features.Auth;

public static class CheckDisplayName
{
    public static async Task<IResult> Handle(
        string name,
        UserManager<EatahUser> userManager,
        CancellationToken ct)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length < AuthValidationRules.DisplayNameMinLength
            || trimmed.Length > AuthValidationRules.DisplayNameMaxLength
            || !AuthValidationRules.DisplayNameRegex.IsMatch(trimmed))
        {
            return Results.Ok(new DisplayNameAvailabilityResponse(trimmed, false));
        }

        var taken = await userManager.Users.AnyAsync(u => u.DisplayName == trimmed, ct);
        return Results.Ok(new DisplayNameAvailabilityResponse(trimmed, !taken));
    }
}
