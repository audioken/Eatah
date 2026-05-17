using Microsoft.JSInterop;

namespace Eatah.WebClient.Services;

/// <summary>
/// Persists the JWT Bearer token in the browser's <c>localStorage</c>.
/// </summary>
public class LocalStorageTokenStore : Eatah.Client.Services.ITokenStore
{
    private const string Key = "eatah_token";
    private readonly IJSRuntime _js;
    private string? _cached;

    public LocalStorageTokenStore(IJSRuntime js)
    {
        _js = js;
    }

    public string? Token => _cached;

    public async Task LoadAsync(CancellationToken ct = default)
    {
        _cached = await _js.InvokeAsync<string?>("localStorage.getItem", Key);
    }

    public void Store(string token)
    {
        _cached = token;
        _ = _js.InvokeVoidAsync("localStorage.setItem", Key, token);
    }

    public void Clear()
    {
        _cached = null;
        _ = _js.InvokeVoidAsync("localStorage.removeItem", Key);
    }
}
