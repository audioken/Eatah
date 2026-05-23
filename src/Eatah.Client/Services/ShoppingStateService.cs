using Eatah.Client.Services.Contracts;

namespace Eatah.Client.Services;

/// <summary>
/// Singleton cache for the shopping list. Fires OnChanged so any component
/// (Shopping.razor, IngredientChecklist) can react to mutations in real-time.
/// Pages should call <see cref="EnsureLoadedAsync"/> instead of hitting the API
/// directly so navigation never causes a refetch when already cached.
/// </summary>
public class ShoppingStateService
{
    private readonly ApiClient _api;
    private List<ShoppingItemResponse> _items = [];
    private bool _loaded;
    private Task<List<ShoppingItemResponse>>? _inflight;
    private readonly object _gate = new();

    public ShoppingStateService(ApiClient api)
    {
        _api = api;
    }

    public IReadOnlyList<ShoppingItemResponse> Items => _items;

    public bool IsLoaded { get { lock (_gate) return _loaded; } }

    /// <summary>Persists expanded weeks/groups across modal opens within a session. Cleared on workspace switch.</summary>
    public HashSet<string> ExpandedWeeks { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> ExpandedGroups { get; } = new(StringComparer.OrdinalIgnoreCase);

    public event Action? OnChanged;

    public Task<List<ShoppingItemResponse>> EnsureLoadedAsync(CancellationToken ct = default)
    {
        lock (_gate)
        {
            if (_loaded) return Task.FromResult(_items);
            if (_inflight is not null) return _inflight;
            _inflight = LoadAsync(ct);
            return _inflight;
        }
    }

    private async Task<List<ShoppingItemResponse>> LoadAsync(CancellationToken ct)
    {
        try
        {
            var list = await _api.GetShoppingListAsync(ct);
            SetShopping(list);
            return list;
        }
        finally
        {
            lock (_gate) _inflight = null;
        }
    }

    public Task RefreshIfLoadedAsync(CancellationToken ct = default)
    {
        lock (_gate)
        {
            if (!_loaded) return Task.CompletedTask;
        }
        return LoadAsync(ct);
    }

    public void SetShopping(List<ShoppingItemResponse> items)
    {
        lock (_gate)
        {
            _items = items;
            _loaded = true;
        }
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

    /// <summary>Clears the cache. Called on workspace switch.</summary>
    public void Clear()
    {
        bool hadAny;
        lock (_gate)
        {
            hadAny = _loaded || _items.Count > 0;
            _items = [];
            _loaded = false;
            _inflight = null;
        }
        ExpandedWeeks.Clear();
        ExpandedGroups.Clear();
        if (hadAny) OnChanged?.Invoke();
    }
}
