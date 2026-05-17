namespace Eatah.Api.Features.Auth;

/// <summary>
/// Settings controlling URLs and behaviour of the auth flow (where confirmation/reset links point).
/// </summary>
public class AuthSettings
{
    public const string SectionName = "Auth";

    /// <summary>
    /// Base URL for confirmation / reset links sent in emails. In dev usually the
    /// MAUI app's loopback host or a placeholder; in prod the public web origin.
    /// </summary>
    public string ClientBaseUrl { get; set; } = "https://localhost:5001";

    /// <summary>
    /// Secret key used to sign JWT tokens. Must be at least 32 characters.
    /// In production, set via the <c>Auth__JwtSecret</c> environment variable.
    /// </summary>
    public string JwtSecret { get; set; } = string.Empty;

    /// <summary>Issuer claim embedded in the token (e.g. "eatah").</summary>
    public string JwtIssuer { get; set; } = "eatah";

    /// <summary>Audience claim embedded in the token (e.g. "eatah").</summary>
    public string JwtAudience { get; set; } = "eatah";

    /// <summary>Token lifetime in days. Defaults to 30.</summary>
    public int JwtExpiryDays { get; set; } = 30;
}

