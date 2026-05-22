using Eatah.Client.Services.Contracts;

namespace Eatah.Client.Services;

/// <summary>
/// Singleton that caches the current workspace pantry and notifies subscribers
/// when items are added or removed. Used for real-time sync between
/// Shopping.razor and IngredientChecklist. Pages should call <see cref="EnsureLoadedAsync"/>
/// instead of hitting the API directly so navigation never causes a refetch when
/// the list is already cached.
/// </summary>
public class PantryStateService
{
    private readonly ApiClient _api;
    private List<PantryItemResponse> _items = [];
    private bool _loaded;
    private Task<List<PantryItemResponse>>? _inflight;
    private readonly object _gate = new();

    public PantryStateService(ApiClient api)
    {
        _api = api;
    }

    public IReadOnlyList<PantryItemResponse> Items => _items;

    public bool IsLoaded { get { lock (_gate) return _loaded; } }

    public event Action? OnChanged;

    public Task<List<PantryItemResponse>> EnsureLoadedAsync(CancellationToken ct = default)
    {
        lock (_gate)
        {
            if (_loaded) return Task.FromResult(_items);
            if (_inflight is not null) return _inflight;
            _inflight = LoadAsync(ct);
            return _inflight;
        }
    }

    private async Task<List<PantryItemResponse>> LoadAsync(CancellationToken ct)
    {
        try
        {
            var list = await _api.GetPantryAsync(ct);
            SetPantry(list);
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

    /// <summary>Replace the full pantry list (called on initial load).</summary>
    public void SetPantry(List<PantryItemResponse> items)
    {
        lock (_gate)
        {
            _items = items;
            _loaded = true;
        }
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
        if (hadAny) OnChanged?.Invoke();
    }
}

