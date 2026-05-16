namespace Eatah.Api.Features.Auth.Email;

/// <summary>
/// Brevo transactional-email HTTP API settings.
/// Set <c>Brevo__ApiKey</c> as an environment variable on the server.
/// When <see cref="IsConfigured"/> is true this sender is preferred over SMTP
/// because it uses HTTPS (port 443) instead of port 587, which is often blocked
/// on cloud-hosting platforms.
/// </summary>
public class BrevoSettings
{
    public const string SectionName = "Brevo";

    public string ApiKey { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
