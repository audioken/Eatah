using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Eatah.Api.Features.AI;

public class AnthropicClient : IAiClient
{
    private const string AnthropicVersion = "2023-06-01";

    private readonly HttpClient _http;
    private readonly AiSettings _settings;
    private readonly ILogger<AnthropicClient> _logger;

    public AnthropicClient(HttpClient http, IOptions<AiSettings> settings, ILogger<AnthropicClient> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.AnthropicApiKey))
        {
            throw new AiServiceException(
                Common.ErrorCodes.AiServiceNotConfigured,
                "Anthropic fallback is not configured. Set AiSettings:AnthropicApiKey.");
        }

        var url = $"{_settings.AnthropicEndpoint.TrimEnd('/')}/messages";

        var payload = new
        {
            model = _settings.AnthropicModel,
            max_tokens = 2048,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = userPrompt }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("x-api-key", _settings.AnthropicApiKey);
        request.Headers.Add("anthropic-version", AnthropicVersion);

        try
        {
            using var response = await _http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Anthropic service responded with status {Status}: {Body}", (int)response.StatusCode, body);
                throw new AiServiceException(
                    Common.ErrorCodes.AiServiceFailure,
                    $"Anthropic service responded with status {(int)response.StatusCode}.");
            }

            using var document = JsonDocument.Parse(body);
            var content = document.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new AiServiceException(
                    Common.ErrorCodes.AiInvalidResponse,
                    "Anthropic service returned an empty response.");
            }

            return content;
        }
        catch (HttpRequestException ex)
        {
            throw new AiServiceException(
                Common.ErrorCodes.AiServiceFailure,
                "Could not reach Anthropic service.", ex);
        }
        catch (JsonException ex)
        {
            throw new AiServiceException(
                Common.ErrorCodes.AiInvalidResponse,
                "Anthropic service returned an invalid response.", ex);
        }
    }
}
