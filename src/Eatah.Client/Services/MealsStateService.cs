using Eatah.Client.Services.Contracts;

namespace Eatah.Client.Services;

/// <summary>
/// Singleton cache for the workspace's meal catalog. Pages read <see cref="Items"/>
/// synchronously and subscribe to <see cref="OnChanged"/>. The list is fetched once
/// per workspace; SignalR <c>MealsChanged</c> events trigger a silent background
/// refetch when another member adds/updates/removes a meal.
/// </summary>
public sealed class MealsStateService
{
    private readonly ApiClient _api;
    private List<MealResponse> _items = [];
    private bool _loaded;
    private Task<List<MealResponse>>? _inflight;
    private readonly object _gate = new();

    public MealsStateService(ApiClient api)
    {
        _api = api;
    }

    public IReadOnlyList<MealResponse> Items
    {
        get { lock (_gate) return _items; }
    }

    public bool IsLoaded { get { lock (_gate) return _loaded; } }

    public event Action? OnChanged;

    public Task<List<MealResponse>> EnsureLoadedAsync(CancellationToken ct = default)
    {
        lock (_gate)
        {
            if (_loaded) return Task.FromResult(_items);
            if (_inflight is not null) return _inflight;
            _inflight = LoadAsync(ct);
            return _inflight;
        }
    }

    private async Task<List<MealResponse>> LoadAsync(CancellationToken ct)
    {
        try
        {
            var list = await _api.GetMealsAsync(ct);
            lock (_gate)
            {
                _items = list;
                _loaded = true;
            }
            OnChanged?.Invoke();
            return list;
        }
        finally
        {
            lock (_gate) _inflight = null;
        }
    }

    /// <summary>Optimistically inserts/updates a meal without refetching. The SignalR
    /// event from the server will also fire but the cache is idempotent.</summary>
    public void AddOrUpdate(MealResponse meal)
    {
        lock (_gate)
        {
            var idx = _items.FindIndex(m => m.Id == meal.Id);
            if (idx >= 0) _items[idx] = meal;
            else _items = [.. _items, meal];
        }
        OnChanged?.Invoke();
    }

    public void Remove(Guid id)
    {
        bool removed;
        lock (_gate) removed = _items.RemoveAll(m => m.Id == id) > 0;
        if (removed) OnChanged?.Invoke();
    }

    /// <summary>Refetches in the background when the list is already loaded — invoked
    /// in response to SignalR <c>MealsChanged</c> so visible UI stays in sync.</summary>
    public Task RefreshIfLoadedAsync(CancellationToken ct = default)
    {
        lock (_gate)
        {
            if (!_loaded) return Task.CompletedTask;
        }
        return LoadAsync(ct);
    }

    /// <summary>Clears the cache. Called on workspace switch so the next read pulls
    /// the new workspace's meals.</summary>
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
