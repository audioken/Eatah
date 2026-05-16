using Microsoft.Maui.Storage;

namespace Eatah.Client.Services;

/// <summary>
/// MAUI-backed <see cref="IUserPreferences"/> using <see cref="Preferences.Default"/>.
/// </summary>
public sealed class MauiUserPreferences : IUserPreferences
{
    public string? Get(string key)
    {
        try
        {
            var raw = Preferences.Default.Get(key, string.Empty);
            return string.IsNullOrEmpty(raw) ? null : raw;
        }
        catch
        {
            return null;
        }
    }

    public void Set(string key, string value)
    {
        try
        {
            Preferences.Default.Set(key, value);
        }
        catch
        {
            // Best-effort persistence.
        }
    }
}
