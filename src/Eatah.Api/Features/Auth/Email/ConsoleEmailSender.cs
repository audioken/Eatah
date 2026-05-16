namespace Eatah.Api.Features.Auth.Email;

/// <summary>
/// Fallback email sender for development. Logs the email contents to the console
/// so developers can copy confirmation / reset links without a real SMTP server.
/// </summary>
public class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toEmail, string subject, string htmlBody, string textBody, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "[DEV EMAIL] To: {To} | Subject: {Subject}\n----- TEXT -----\n{Text}\n----------------",
            toEmail, subject, textBody);
        return Task.CompletedTask;
    }
}
