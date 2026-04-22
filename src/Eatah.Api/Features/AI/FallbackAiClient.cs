namespace Eatah.Api.Features.AI;

public class FallbackAiClient : IAiClient
{
    private readonly GeminiClient _primary;
    private readonly AnthropicClient _fallback;
    private readonly ILogger<FallbackAiClient> _logger;

    public FallbackAiClient(
        GeminiClient primary,
        AnthropicClient fallback,
        ILogger<FallbackAiClient> logger)
    {
        _primary = primary;
        _fallback = fallback;
        _logger = logger;
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        try
        {
            return await _primary.CompleteAsync(systemPrompt, userPrompt, cancellationToken);
        }
        catch (AiServiceException ex) when (ex.Code == Common.ErrorCodes.AiServiceFailure)
        {
            _logger.LogWarning("Primary AI (Gemini) unavailable ({Message}), falling back to Anthropic Haiku.", ex.Message);
            return await _fallback.CompleteAsync(systemPrompt, userPrompt, cancellationToken);
        }
    }
}
