using Eatah.Client.Services.Contracts;

namespace Eatah.Client.Services;

/// <summary>
/// Singleton cache for the shopping list. Fires OnChanged so any component
/// (Shopping.razor, IngredientChecklist) can react to mutations in real-time.
/// </summary>
public class ShoppingStateService
{
    private List<ShoppingItemResponse> _items = [];

    public IReadOnlyList<ShoppingItemResponse> Items => _items;

    public event Action? OnChanged;

    public void SetShopping(List<ShoppingItemResponse> items)
    {
        _items = items;
        OnChanged?.Invoke();
    }

    public void AddOrUpdateItem(ShoppingItemResponse item)
    {
        var idx = _items.FindIndex(s => s.IngredientId == item.IngredientId);
        if (idx < 0)
            _items.Insert(0, item);
        else
            _items[idx] = item;
        OnChanged?.Invoke();
    }

    public void RemoveItem(Guid id)
    {
        var removed = _items.RemoveAll(s => s.Id == id);
        if (removed > 0) OnChanged?.Invoke();
    }

    /// <summary>Removes all shopping items whose IngredientId matches — used when
    /// an ingredient is moved to pantry from the IngredientChecklist.</summary>
    public void RemoveByIngredientId(Guid ingredientId)
    {
        var removed = _items.RemoveAll(s => s.IngredientId == ingredientId);
        if (removed > 0) OnChanged?.Invoke();
    }

    public void UpdateItem(ShoppingItemResponse item)
    {
        var idx = _items.FindIndex(s => s.Id == item.Id);
        if (idx >= 0) { _items[idx] = item; OnChanged?.Invoke(); }
    }

    public void RemoveChecked()
    {
        var removed = _items.RemoveAll(s => s.IsChecked);
        if (removed > 0) OnChanged?.Invoke();
    }
}
