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
}
