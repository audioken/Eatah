using Eatah.Api.Common;
using Eatah.Api.Features.Workspaces;
using Eatah.Infrastructure.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Eatah.Api.Features.Auth;

public static class ConfirmEmailAndSetCredentials
{
    public static async Task<IResult> Handle(
        ConfirmEmailRequest request,
        IValidator<ConfirmEmailRequest> validator,
        UserManager<EatahUser> userManager,
        SignInManager<EatahUser> signInManager,
        WorkspaceService workspaceService,
        IOptions<AuthSettings> authSettings,
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
                .Failure(Error.NotFound(ErrorCodes.AuthUserNotFound, "User not found."))
                .ToHttpResult();
        }

        if (user.EmailConfirmed)
        {
            return Result<UserResponse>
                .Failure(Error.Conflict(ErrorCodes.AuthInvalidToken, "Email is already confirmed."))
                .ToHttpResult();
        }

        if (!TokenEncoding.TryDecode(request.Token, out var rawToken))
        {
            return Result<UserResponse>
                .Failure(Error.BadRequest(ErrorCodes.AuthInvalidToken, "Confirmation token is invalid."))
                .ToHttpResult();
        }

        var confirm = await userManager.ConfirmEmailAsync(user, rawToken);
        if (!confirm.Succeeded)
        {
            return Result<UserResponse>
                .Failure(Error.BadRequest(ErrorCodes.AuthInvalidToken, "Confirmation token is invalid or expired."))
                .ToHttpResult();
        }

        // DisplayName uniqueness
        var trimmed = request.DisplayName.Trim();
        var nameTaken = await userManager.Users.AnyAsyncSafe(u => u.DisplayName == trimmed && u.Id != user.Id, ct);
        if (nameTaken)
        {
            return Result<UserResponse>
                .Failure(Error.Conflict(ErrorCodes.AuthDisplayNameTaken, "Display name is already taken."))
                .ToHttpResult();
        }

        user.DisplayName = trimmed;
        user.UserName = trimmed;
        var updateName = await userManager.UpdateAsync(user);
        if (!updateName.Succeeded)
        {
            return Result<UserResponse>
                .Failure(Error.BadRequest(ErrorCodes.AuthDisplayNameTaken, updateName.Errors.First().Description))
                .ToHttpResult();
        }

        var setPwd = await userManager.AddPasswordAsync(user, request.Password);
        if (!setPwd.Succeeded)
        {
            return Result<UserResponse>
                .Failure(Error.BadRequest(ErrorCodes.AuthPasswordInvalid, setPwd.Errors.First().Description))
                .ToHttpResult();
        }

        await signInManager.SignInAsync(user, isPersistent: true);

        // Ensure the user has a household (idempotent). Created as a 1-person "Mitt hushåll"
        // so the app is immediately usable; the user can rename it in the profile menu.
        await workspaceService.EnsureDefaultHouseholdAsync(user.Id, ct);

        var token = JwtTokenHelper.GenerateToken(user, authSettings.Value);
        return Results.Ok(new AuthResponse(user.Id, user.Email!, user.DisplayName, token));
    }
}

internal static class UserQueryExtensions
{
    // Small wrapper so we don't have to bring Microsoft.EntityFrameworkCore into every endpoint file.
    public static async Task<bool> AnyAsyncSafe<T>(this IQueryable<T> source, System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken ct)
        => await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(source, predicate, ct);
}
