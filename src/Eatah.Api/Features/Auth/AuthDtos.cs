namespace Eatah.Api.Features.Auth;

// ----- Requests -----

public record RegisterEmailRequest(string Email);

public record ConfirmEmailRequest(Guid UserId, string Token, string DisplayName, string Password);

public record LoginRequest(string EmailOrUsername, string Password, bool RememberMe);

public record RequestPasswordResetRequest(string Email);

public record ResetPasswordRequest(Guid UserId, string Token, string NewPassword);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record UpdateProfileRequest(string? DisplayName, string? Email);

public record DeleteAccountRequest(string Password);

// ----- Responses -----

public record UserResponse(Guid Id, string Email, string DisplayName);

/// <summary>Returned by login, email confirmation and password reset. Includes a JWT for header-based auth clients.</summary>
public record AuthResponse(Guid Id, string Email, string DisplayName, string Token);

public record RegistrationResponse(Guid UserId, string Email, bool ConfirmationEmailSent);

public record DisplayNameAvailabilityResponse(string DisplayName, bool Available);
