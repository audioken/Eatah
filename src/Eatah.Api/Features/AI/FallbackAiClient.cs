using Microsoft.Extensions.Options;

namespace Eatah.Api.Features.AI;

public class FallbackAiClient : IAiClient
{
    private readonly GeminiClient _primary;
    private readonly AnthropicClient _fallback;
    private readonly AiSettings _settings;
    private readonly ILogger<FallbackAiClient> _logger;

    public FallbackAiClient(
        GeminiClient primary,
        AnthropicClient fallback,
        IOptions<AiSettings> settings,
        ILogger<FallbackAiClient> logger)
    {
        _primary = primary;
        _fallback = fallback;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken, float temperature = 0.7f)
    {
        try
        {
            return await _primary.CompleteAsync(systemPrompt, userPrompt, cancellationToken, temperature);
        }
        catch (AiServiceException ex) when (ex.Code == Common.ErrorCodes.AiServiceFailure)
        {
            if (string.IsNullOrWhiteSpace(_settings.AnthropicApiKey))
            {
                _logger.LogWarning("Primary AI (Gemini) failed: {Message}. Anthropic fallback is not configured — rethrowing.", ex.Message);
                throw;
            }

            _logger.LogWarning("Primary AI (Gemini) unavailable ({Message}), falling back to Anthropic Haiku.", ex.Message);
            return await _fallback.CompleteAsync(systemPrompt, userPrompt, cancellationToken, temperature);
        }
    }
}
