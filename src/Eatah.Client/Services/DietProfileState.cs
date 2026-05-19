namespace Eatah.Client.Services;

/// <summary>
/// Singleton that broadcasts when the diet profile list has changed (created/deleted)
/// and holds the currently selected profile id so all components (selector card,
/// day-level randomize, AI generation) operate on the same profile.
/// </summary>
public sealed class DietProfileState
{
    public event Action? OnChanged;

    public Guid? SelectedProfileId { get; private set; }

    public void SetSelectedProfileId(Guid? id) => SelectedProfileId = id;

    public void NotifyChanged() => OnChanged?.Invoke();
}
