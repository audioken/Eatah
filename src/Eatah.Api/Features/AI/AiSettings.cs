namespace Eatah.Api.Features.AI;

public class AiSettings
{
    public const string SectionName = "AiSettings";

    // Gemini (primary)
    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-2.5-flash";
    public int TimeoutSeconds { get; set; } = 30;

    // Anthropic (fallback)
    public string AnthropicEndpoint { get; set; } = "https://api.anthropic.com/v1";
    public string AnthropicApiKey { get; set; } = string.Empty;
    public string AnthropicModel { get; set; } = "claude-haiku-4-5";
}
