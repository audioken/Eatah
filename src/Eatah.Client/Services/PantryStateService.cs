using Eatah.Client.Services.Contracts;

namespace Eatah.Client.Services;

/// <summary>
/// Singleton that caches the current workspace pantry and notifies subscribers
/// when items are added or removed. Used for real-time sync between
/// Shopping.razor and IngredientChecklist.
/// </summary>
public class PantryStateService
{
    private List<PantryItemResponse> _items = [];

    public IReadOnlyList<PantryItemResponse> Items => _items;

    public event Action? OnChanged;

    /// <summary>Replace the full pantry list (called on initial load).</summary>
    public void SetPantry(List<PantryItemResponse> items)
    {
        _items = items;
        OnChanged?.Invoke();
    }

    /// <summary>Add a single item (called after a successful AddToPantry API call).</summary>
    public void AddItem(PantryItemResponse item)
    {
        if (!_items.Any(p => p.IngredientId == item.IngredientId))
        {
            _items.Insert(0, item);
            OnChanged?.Invoke();
        }
    }

    /// <summary>Remove a pantry item by its pantry-row id.</summary>
    public void RemoveItem(Guid id)
    {
        var removed = _items.RemoveAll(p => p.Id == id);
        if (removed > 0) OnChanged?.Invoke();
    }
}
