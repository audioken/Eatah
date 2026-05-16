namespace Eatah.Client.Services;

/// <summary>
/// Simple key/value preference store. Abstracts platform-specific persistence
/// (MAUI <c>Preferences</c> on native, <c>localStorage</c> on web).
/// </summary>
public interface IUserPreferences
{
    string? Get(string key);
    void Set(string key, string value);
}
