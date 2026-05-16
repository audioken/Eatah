using Microsoft.JSInterop;
using Eatah.Client.Services;

namespace Eatah.WebClient.Services;

/// <summary>
/// <see cref="IUserPreferences"/> backed by browser <c>localStorage</c>. Falls back
/// to an in-memory dictionary when JS interop is unavailable (e.g. prerender).
/// </summary>
public sealed class LocalStorageUserPreferences : IUserPreferences
{
    private readonly IJSInProcessRuntime _js;
    private readonly Dictionary<string, string> _fallback = new();

    public LocalStorageUserPreferences(IJSRuntime js)
    {
        _js = (IJSInProcessRuntime)js;
    }

    public string? Get(string key)
    {
        try
        {
            var value = _js.Invoke<string?>("localStorage.getItem", key);
            return string.IsNullOrEmpty(value) ? null : value;
        }
        catch
        {
            return _fallback.TryGetValue(key, out var value) ? value : null;
        }
    }

    public void Set(string key, string value)
    {
        try
        {
            _js.InvokeVoid("localStorage.setItem", key, value);
        }
        catch
        {
            _fallback[key] = value;
        }
    }
}
