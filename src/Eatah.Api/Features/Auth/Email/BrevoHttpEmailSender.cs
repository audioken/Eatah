using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Eatah.Api.Features.Auth.Email;

/// <summary>
/// Sends transactional emails via Brevo's REST API (HTTPS port 443).
/// Preferred over <see cref="SmtpEmailSender"/> when running on platforms
/// that block outbound SMTP ports (587 / 465).
/// </summary>
public class BrevoHttpEmailSender : IEmailSender
{
    private const string ApiUrl = "https://api.brevo.com/v3/smtp/email";

    private readonly BrevoSettings _settings;
    private readonly SmtpSettings _smtpSettings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BrevoHttpEmailSender> _logger;

    public BrevoHttpEmailSender(
        IOptions<BrevoSettings> brevoSettings,
        IOptions<SmtpSettings> smtpSettings,
        IHttpClientFactory httpClientFactory,
        ILogger<BrevoHttpEmailSender> logger)
    {
        _settings = brevoSettings.Value;
        _smtpSettings = smtpSettings.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken ct = default)
    {
        var payload = new
        {
            sender = new { email = _smtpSettings.FromEmail, name = _smtpSettings.FromName },
            to = new[] { new { email = toEmail } },
            subject,
            htmlContent = htmlBody,
            textContent = textBody
        };

        var json = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
        request.Headers.Add("api-key", _settings.ApiKey);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        var response = await client.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Brevo API returned {StatusCode}: {Body}", (int)response.StatusCode, body);
            throw new InvalidOperationException($"Brevo email API failed with status {(int)response.StatusCode}: {body}");
        }

        _logger.LogInformation("Sent email via Brevo API to {Recipient} with subject {Subject}", toEmail, subject);
    }
}
