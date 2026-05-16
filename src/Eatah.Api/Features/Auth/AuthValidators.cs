using System.Text.RegularExpressions;
using FluentValidation;

namespace Eatah.Api.Features.Auth;

public static class AuthValidationRules
{
    // Password rules (mirrored by client-side live validation): >=8, upper, lower, digit, special.
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;
    public const int DisplayNameMinLength = 3;
    public const int DisplayNameMaxLength = 50;

    public static readonly Regex DisplayNameRegex = new("^[a-zA-Z0-9_\\-]+$", RegexOptions.Compiled);

    public static IRuleBuilderOptions<T, string> Password<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(PasswordMinLength).WithMessage($"Password must be at least {PasswordMinLength} characters.")
            .MaximumLength(PasswordMaxLength).WithMessage($"Password must be at most {PasswordMaxLength} characters.")
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain a special character.");

    public static IRuleBuilderOptions<T, string> DisplayName<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("Display name is required.")
            .MinimumLength(DisplayNameMinLength).WithMessage($"Display name must be at least {DisplayNameMinLength} characters.")
            .MaximumLength(DisplayNameMaxLength).WithMessage($"Display name must be at most {DisplayNameMaxLength} characters.")
            .Matches(DisplayNameRegex).WithMessage("Display name may only contain letters, digits, '_' and '-'.");

    public static IRuleBuilderOptions<T, string> Email<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not in a valid format.")
            .MaximumLength(254).WithMessage("Email must be at most 254 characters.");
}

public class RegisterEmailValidator : AbstractValidator<RegisterEmailRequest>
{
    public RegisterEmailValidator() => RuleFor(x => x.Email).Email();
}

public class ConfirmEmailValidator : AbstractValidator<ConfirmEmailRequest>
{
    public ConfirmEmailValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User id is required.");
        RuleFor(x => x.Token).NotEmpty().WithMessage("Token is required.");
        RuleFor(x => x.DisplayName).DisplayName();
        RuleFor(x => x.Password).Password();
    }
}

public class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.EmailOrUsername).NotEmpty().WithMessage("Email or username is required.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
    }
}

public class RequestPasswordResetValidator : AbstractValidator<RequestPasswordResetRequest>
{
    public RequestPasswordResetValidator() => RuleFor(x => x.Email).Email();
}

public class ResetPasswordValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User id is required.");
        RuleFor(x => x.Token).NotEmpty().WithMessage("Token is required.");
        RuleFor(x => x.NewPassword).Password();
    }
}

public class ChangePasswordValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Current password is required.");
        RuleFor(x => x.NewPassword).Password();
    }
}
