using Eatah.Client.Services.Contracts;

namespace Eatah.Client.Services;

/// <summary>
/// Singleton cache for weekly plans, keyed by (year, weekNumber). Pages read from
/// the cache synchronously and subscribe to <see cref="OnChanged"/>; refetches happen
/// in the background when a SignalR <c>WeeklyPlanChanged</c> event arrives or when a
/// mutation has invalidated a week. Concurrent <see cref="EnsureLoadedAsync"/> calls
/// for the same week share a single inflight task so we never duplicate network IO.
/// </summary>
public sealed class WeeklyPlanStateService
{
    private readonly ApiClient _api;
    private readonly Dictionary<(int year, int week), WeeklyPlanResponse> _cache = new();
    private readonly Dictionary<(int year, int week), Task<WeeklyPlanResponse?>> _inflight = new();
    private readonly object _gate = new();

    public WeeklyPlanStateService(ApiClient api)
    {
        _api = api;
    }

    /// <summary>Fired when a week's plan in the cache changes. Args: (year, week).</summary>
    public event Action<int, int>? OnChanged;

    public WeeklyPlanResponse? TryGet(int year, int week)
    {
        lock (_gate)
        {
            return _cache.TryGetValue((year, week), out var plan) ? plan : null;
        }
    }

    /// <summary>
    /// Returns the cached plan immediately if present (without refetching), otherwise
    /// fetches once. Concurrent callers for the same week share the inflight request.
    /// </summary>
    public Task<WeeklyPlanResponse?> EnsureLoadedAsync(int year, int week, CancellationToken ct = default)
    {
        lock (_gate)
        {
            if (_cache.TryGetValue((year, week), out var cached))
                return Task.FromResult<WeeklyPlanResponse?>(cached);
            if (_inflight.TryGetValue((year, week), out var pending))
                return pending;

            var task = LoadAsync(year, week, ct);
            _inflight[(year, week)] = task;
            return task;
        }
    }

    private async Task<WeeklyPlanResponse?> LoadAsync(int year, int week, CancellationToken ct)
    {
        try
        {
            var plan = await _api.GetWeeklyPlanByWeekAsync(year, week, ct);
            if (plan is not null) Set(plan);
            return plan;
        }
        finally
        {
            lock (_gate) _inflight.Remove((year, week));
        }
    }

    /// <summary>Writes a fresh plan into the cache and notifies subscribers. Use after
    /// a mutation returns the updated aggregate so other listeners see it immediately.</summary>
    public void Set(WeeklyPlanResponse plan)
    {
        lock (_gate) _cache[(plan.Year, plan.WeekNumber)] = plan;
        OnChanged?.Invoke(plan.Year, plan.WeekNumber);
    }

    /// <summary>Drops a cached week so the next read triggers a refetch.</summary>
    public void Invalidate(int year, int week)
    {
        bool removed;
        lock (_gate) removed = _cache.Remove((year, week));
        if (removed) OnChanged?.Invoke(year, week);
    }

    /// <summary>Triggers a background refetch for a week if (and only if) it is cached.
    /// Used in response to SignalR <c>WeeklyPlanChanged</c> events so we silently
    /// refresh visible data without showing a spinner.</summary>
    public Task RefreshIfCachedAsync(int year, int week, CancellationToken ct = default)
    {
        lock (_gate)
        {
            if (!_cache.ContainsKey((year, week))) return Task.CompletedTask;
        }
        return LoadAsync(year, week, ct);
    }

    /// <summary>Clears every cached week. Called on workspace switch.</summary>
    public void Clear()
    {
        bool hadAny;
        lock (_gate)
        {
            hadAny = _cache.Count > 0;
            _cache.Clear();
            _inflight.Clear();
        }
        if (hadAny) OnChanged?.Invoke(0, 0);
    }
}
