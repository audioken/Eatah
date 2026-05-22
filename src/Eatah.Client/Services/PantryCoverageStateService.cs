using Eatah.Client.Services.Contracts;

namespace Eatah.Client.Services;

/// <summary>
/// Singleton cache for pantry coverage (which pantry items cover which day-plans).
/// Coverage is workspace-wide so it is cached as a single list; mutations to either
/// the pantry or the weekly plan can invalidate it, so SignalR <c>PantryChanged</c>
/// and <c>WeeklyPlanChanged</c> both trigger a silent refresh.
/// </summary>
public sealed class PantryCoverageStateService
{
    private readonly ApiClient _api;
    private List<PantryCoverageResponse> _items = [];
    private bool _loaded;
    private Task<List<PantryCoverageResponse>>? _inflight;
    private readonly object _gate = new();

    public PantryCoverageStateService(ApiClient api)
    {
        _api = api;
    }

    public IReadOnlyList<PantryCoverageResponse> Items
    {
        get { lock (_gate) return _items; }
    }

    public event Action? OnChanged;

    public Task<List<PantryCoverageResponse>> EnsureLoadedAsync(CancellationToken ct = default)
    {
        lock (_gate)
        {
            if (_loaded) return Task.FromResult(_items);
            if (_inflight is not null) return _inflight;
            _inflight = LoadAsync(ct);
            return _inflight;
        }
    }

    private async Task<List<PantryCoverageResponse>> LoadAsync(CancellationToken ct)
    {
        try
        {
            var list = await _api.GetPantryCoverageAsync(ct);
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

    public Task RefreshIfLoadedAsync(CancellationToken ct = default)
    {
        lock (_gate)
        {
            if (!_loaded) return Task.CompletedTask;
        }
        return LoadAsync(ct);
    }

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
