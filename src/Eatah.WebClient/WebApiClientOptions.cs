namespace Eatah.WebClient;

public static class WebApiClientOptions
{
    public static Uri GetBaseAddress(string configuredUrl)
        => new(configuredUrl);
}
