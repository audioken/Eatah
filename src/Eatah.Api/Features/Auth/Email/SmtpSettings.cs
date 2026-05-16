namespace Eatah.Api.Features.Auth.Email;

/// <summary>
/// SMTP configuration. Values populated from <c>SmtpSettings</c> section in appsettings/user-secrets.
/// In development with no Host configured, the system falls back to <see cref="ConsoleEmailSender"/>.
/// </summary>
public class SmtpSettings
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "noreply@eatah.local";
    public string FromName { get; set; } = "Eatah";
    public bool UseSsl { get; set; } = false;

    /// <summary>True when at least a host is configured (i.e. real SMTP should be used).</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);
}
