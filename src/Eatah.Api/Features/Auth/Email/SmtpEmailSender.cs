using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Eatah.Api.Features.Auth.Email;

/// <summary>
/// SMTP-based email sender (MailKit). Used when <see cref="SmtpSettings.IsConfigured"/> is true.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpSettings> settings, ILogger<SmtpEmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, string textBody, CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = htmlBody,
            TextBody = textBody
        };
        message.Body = bodyBuilder.ToMessageBody();

        // Hard cap of 30 s – prevents indefinite hangs in containers where
        // libgssapi_krb5.so.2 is missing and GSSAPI negotiation stalls.
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(30));
        var smtpCt = cts.Token;

        using var client = new SmtpClient();

        // Remove auth mechanisms that require Kerberos / NTLM – not available on Linux containers.
        client.AuthenticationMechanisms.Remove("GSSAPI");
        client.AuthenticationMechanisms.Remove("NTLM");

        var secureOption = _settings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
        await client.ConnectAsync(_settings.Host, _settings.Port, secureOption, smtpCt);
        if (!string.IsNullOrEmpty(_settings.Username))
        {
            await client.AuthenticateAsync(_settings.Username, _settings.Password, smtpCt);
        }
        await client.SendAsync(message, smtpCt);
        await client.DisconnectAsync(true, smtpCt);

        _logger.LogInformation("Sent email to {Recipient} with subject {Subject}", toEmail, subject);
    }
}
