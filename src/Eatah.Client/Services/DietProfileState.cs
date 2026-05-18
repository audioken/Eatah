namespace Eatah.Client.Services;

/// <summary>
/// Singleton that broadcasts when the diet profile list has changed (created/deleted).
/// Components that display the profile list subscribe here so they can refresh
/// without requiring a full page reload.
/// </summary>
public sealed class DietProfileState
{
    public event Action? OnChanged;

    public void NotifyChanged() => OnChanged?.Invoke();
}
