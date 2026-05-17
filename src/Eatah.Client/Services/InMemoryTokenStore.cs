namespace Eatah.Client.Services;

/// <summary>
/// In-memory token store for MAUI. The token is kept only for the current app session;
/// MAUI's <c>HttpClientHandler</c> with a <see cref="System.Net.CookieContainer"/> handles
/// cookie-based persistence alongside the JWT.
/// </summary>
public class InMemoryTokenStore : ITokenStore
{
    public string? Token { get; private set; }

    public Task LoadAsync(CancellationToken ct = default) => Task.CompletedTask;

    public void Store(string token) => Token = token;

    public void Clear() => Token = null;
}
