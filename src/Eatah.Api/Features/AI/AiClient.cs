using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Eatah.Api.Features.AI;

public class AiServiceException : Exception
{
    public string Code { get; }

    public AiServiceException(string code, string message) : base(message)
    {
        Code = code;
    }

    public AiServiceException(string code, string message, Exception innerException) : base(message, innerException)
    {
        Code = code;
    }
}

public interface IAiClient
{
    Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken, float temperature = 0.7f);
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

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken, float temperature = 0.7f)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new AiServiceException(
                Common.ErrorCodes.AiServiceNotConfigured,
                "AI service is not configured. Set AiSettings:ApiKey.");
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
                temperature = (double)temperature,
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
                _logger.LogWarning("AI service responded with status {Status}: {Body}", (int)response.StatusCode, body);
                throw new AiServiceException(
                    Common.ErrorCodes.AiServiceFailure,
                    $"AI service responded with status {(int)response.StatusCode}.");
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
                throw new AiServiceException(
                    Common.ErrorCodes.AiInvalidResponse,
                    "AI service returned an empty response.");
            }

            return content;
        }
        catch (HttpRequestException ex)
        {
            throw new AiServiceException(
                Common.ErrorCodes.AiServiceFailure,
                "Could not reach AI service.", ex);
        }
        catch (JsonException ex)
        {
            throw new AiServiceException(
                Common.ErrorCodes.AiInvalidResponse,
                "AI service returned an invalid response.", ex);
        }
    }
}
