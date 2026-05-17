namespace Eatah.Client.Services;

/// <summary>
/// Abstraction over where the JWT Bearer token is stored.
/// WebClient persists to localStorage; MAUI uses an in-memory no-op store.
/// </summary>
public interface ITokenStore
{
    /// <summary>The cached token, or <c>null</c> if none is loaded/stored.</summary>
    string? Token { get; }

    /// <summary>
    /// Loads the token from persistent storage into memory.
    /// Must be called once during app startup before the first API request.
    /// </summary>
    Task LoadAsync(CancellationToken ct = default);

    /// <summary>Persists and caches the given token.</summary>
    void Store(string token);

    /// <summary>Removes the token from memory and persistent storage.</summary>
    void Clear();
}
