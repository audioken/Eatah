namespace Eatah.Api.Features.AI;

public class AiSettings
{
    public const string SectionName = "AiSettings";

    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-2.0-flash";
    public int TimeoutSeconds { get; set; } = 30;
}
