namespace Eatah.Api.Features.Auth;

// ----- Requests -----

public record RegisterEmailRequest(string Email);

public record ConfirmEmailRequest(Guid UserId, string Token, string DisplayName, string Password);

public record LoginRequest(string EmailOrUsername, string Password, bool RememberMe);

public record RequestPasswordResetRequest(string Email);

public record ResetPasswordRequest(Guid UserId, string Token, string NewPassword);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

// ----- Responses -----

public record UserResponse(Guid Id, string Email, string DisplayName);

public record RegistrationResponse(Guid UserId, string Email, bool ConfirmationEmailSent);

public record DisplayNameAvailabilityResponse(string DisplayName, bool Available);
