namespace Eatah.WebClient;

/// <summary>
/// Resolves the API base address for the Blazor WASM web client. In development this
/// points at the public deployment, mirroring <c>Eatah.Client</c>'s behavior. In production
/// (when hosted from a known origin) we still call out to the same API.
/// </summary>
public static class WebApiClientOptions
{
    public static Uri GetBaseAddress(string hostBaseAddress)
        => new("https://eatah.onrender.com/");
}
