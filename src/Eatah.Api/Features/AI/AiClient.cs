using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Eatah.Api.Features.AI;

public class AiServiceException : Exception
{
    public AiServiceException(string message) : base(message) { }
    public AiServiceException(string message, Exception innerException) : base(message, innerException) { }
}

public interface IAiClient
{
    Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken);
}

public class GeminiClient : IAiClient
{
    private readonly HttpClient _http;
    private readonly AiSettings _settings;
    private readonly ILogger<GeminiClient> _logger;

    public GeminiClient(HttpClient http, IOptions<AiSettings> settings, ILogger<GeminiClient> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new AiServiceException("AI-tjänsten är inte konfigurerad. Ange AiSettings:ApiKey.");
        }

        var url = $"{_settings.Endpoint.TrimEnd('/')}/models/{_settings.Model}:generateContent?key={_settings.ApiKey}";

        var payload = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = systemPrompt } }
            },
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = userPrompt } }
                }
            },
            generationConfig = new
            {
                temperature = 0.2,
                responseMimeType = "application/json"
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload)
        };

        try
        {
            using var response = await _http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("AI-tjänsten svarade med status {Status}: {Body}", (int)response.StatusCode, body);
                throw new AiServiceException($"AI-tjänsten svarade med status {(int)response.StatusCode}.");
            }

            using var document = JsonDocument.Parse(body);
            var content = document.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new AiServiceException("AI-tjänsten returnerade ett tomt svar.");
            }

            return content;
        }
        catch (HttpRequestException ex)
        {
            throw new AiServiceException("Kunde inte nå AI-tjänsten.", ex);
        }
        catch (JsonException ex)
        {
            throw new AiServiceException("AI-tjänsten returnerade ett ogiltigt svar.", ex);
        }
    }
}
