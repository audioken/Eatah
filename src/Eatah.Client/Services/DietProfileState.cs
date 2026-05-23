using Eatah.Client.Services.Contracts;

namespace Eatah.Client.Services;

/// <summary>
/// Singleton that broadcasts when the diet profile list has changed (created/deleted)
/// and holds the currently selected profile id so all components (selector card,
/// day-level randomize, AI generation) operate on the same profile.
/// <para>
/// Also caches diet-plan evaluations keyed by
/// <c>(planId, profileId, planSignature)</c> so that switching back to a
/// previously-viewed week never triggers a redundant API call, while a mutation
/// (randomize, assign, clear) always produces a new signature and therefore a fresh
/// fetch — no explicit invalidation needed.
/// </para>
/// </summary>
public sealed class DietProfileState
{
    private readonly ApiClient _api;
    private readonly Dictionary<(Guid planId, Guid profileId, string sig), DietEvaluationResponse> _evalCache = new();
    private readonly Dictionary<(Guid planId, Guid profileId, string sig), Task<DietEvaluationResponse?>> _evalInflight = new();
    private readonly object _gate = new();

    public DietProfileState(ApiClient api)
    {
        _api = api;
    }

    public event Action? OnChanged;

    public Guid? SelectedProfileId { get; private set; }

    public void SetSelectedProfileId(Guid? id) => SelectedProfileId = id;

    public void NotifyChanged() => OnChanged?.Invoke();

    /// <summary>
    /// Returns the cached evaluation instantly when the same
    /// <paramref name="planId"/> + <paramref name="profileId"/> +
    /// <paramref name="planSignature"/> triple was seen before. Otherwise fetches
    /// once, deduplicating concurrent callers.
    /// <para>
    /// Because the signature encodes the plan's meal composition, any mutation
    /// (randomize, assign, clear) naturally produces a cache miss — no manual
    /// invalidation is required.
    /// </para>
    /// </summary>
    public Task<DietEvaluationResponse?> EnsureEvaluationAsync(Guid planId, Guid profileId, string planSignature)
    {
        lock (_gate)
        {
            var key = (planId, profileId, planSignature);
            if (_evalCache.TryGetValue(key, out var cached))
                return Task.FromResult<DietEvaluationResponse?>(cached);
            if (_evalInflight.TryGetValue(key, out var pending))
                return pending;

            var task = FetchEvaluationAsync(planId, profileId, planSignature);
            _evalInflight[key] = task;
            return task;
        }
    }

    private async Task<DietEvaluationResponse?> FetchEvaluationAsync(Guid planId, Guid profileId, string planSignature)
    {
        try
        {
            var result = await _api.EvaluateWeeklyPlanAsync(planId, profileId);
            if (result is not null)
                lock (_gate) _evalCache[(planId, profileId, planSignature)] = result;
            return result;
        }
        finally
        {
            lock (_gate) _evalInflight.Remove((planId, profileId, planSignature));
        }
    }

    /// <summary>Clears all cached evaluations. Call on workspace switch.</summary>
    public void ClearEvaluations()
    {
        lock (_gate)
        {
            _evalCache.Clear();
            _evalInflight.Clear();
        }
    }
}
