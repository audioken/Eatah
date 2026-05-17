using Eatah.Api.Common;
using Eatah.Infrastructure.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Eatah.Api.Features.Auth;

public static class Login
{
    public static async Task<IResult> Handle(
        LoginRequest request,
        IValidator<LoginRequest> validator,
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

        var identifier = request.EmailOrUsername.Trim();
        var user = identifier.Contains('@')
            ? await userManager.FindByEmailAsync(identifier)
            : await userManager.FindByNameAsync(identifier);

        if (user is null)
        {
            return Result<AuthResponse>
                .Failure(Error.BadRequest(ErrorCodes.AuthInvalidCredentials, "Invalid email/username or password."))
                .ToHttpResult();
        }

        if (!user.EmailConfirmed)
        {
            return Result<AuthResponse>
                .Failure(Error.BadRequest(ErrorCodes.AuthEmailNotConfirmed, "Email address has not been confirmed."))
                .ToHttpResult();
        }

        var signIn = await signInManager.PasswordSignInAsync(user, request.Password, request.RememberMe, lockoutOnFailure: true);
        if (!signIn.Succeeded)
        {
            return Result<AuthResponse>
                .Failure(Error.BadRequest(ErrorCodes.AuthInvalidCredentials, "Invalid email/username or password."))
                .ToHttpResult();
        }

        var token = JwtTokenHelper.GenerateToken(user, authSettings.Value);
        return Results.Ok(new AuthResponse(user.Id, user.Email!, user.DisplayName, token));
    }
}
