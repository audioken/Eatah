namespace Eatah.Client.Services;

/// <summary>
/// Singleton that listens for workspace-scoped invalidation events from
/// <see cref="ChatHubService"/> and silently refreshes the affected client caches
/// when the change happened in the currently active workspace. It also clears
/// every cache on workspace switch so cross-tenant data never leaks into the UI.
/// <para>
/// Refreshes are stale-while-revalidate: caches only refetch if already loaded,
/// so unopened pages don't trigger network traffic. Cross-workspace events are
/// ignored.
/// </para>
/// </summary>
public sealed class RealtimeSyncService : IDisposable
{
    private readonly ChatHubService _hub;
    private readonly WorkspaceState _workspace;
    private readonly PantryStateService _pantry;
    private readonly ShoppingStateService _shopping;
    private readonly WeeklyPlanStateService _weeklyPlans;
    private readonly MealsStateService _meals;
    private readonly PantryCoverageStateService _coverage;
    private bool _started;

    public RealtimeSyncService(
        ChatHubService hub,
        WorkspaceState workspace,
        PantryStateService pantry,
        ShoppingStateService shopping,
        WeeklyPlanStateService weeklyPlans,
        MealsStateService meals,
        PantryCoverageStateService coverage)
    {
        _hub = hub;
        _workspace = workspace;
        _pantry = pantry;
        _shopping = shopping;
        _weeklyPlans = weeklyPlans;
        _meals = meals;
        _coverage = coverage;
    }

    public void Start()
    {
        if (_started) return;
        _started = true;
        _hub.PantryChanged += OnPantryChanged;
        _hub.ShoppingListChanged += OnShoppingChanged;
        _hub.WeeklyPlanChanged += OnWeeklyPlanChanged;
        _hub.MealsChanged += OnMealsChanged;
        _workspace.OnChange += OnWorkspaceChanged;
    }

    private void OnPantryChanged(Guid workspaceId)
    {
        if (_workspace.CurrentId != workspaceId) return;
        _ = _pantry.RefreshIfLoadedAsync();
        // Pantry contents drive coverage — refresh that too if anyone is watching.
        _ = _coverage.RefreshIfLoadedAsync();
    }

    private void OnShoppingChanged(Guid workspaceId)
    {
        if (_workspace.CurrentId != workspaceId) return;
        _ = _shopping.RefreshIfLoadedAsync();
    }

    private void OnWeeklyPlanChanged(Guid workspaceId, Guid planId, int year, int weekNumber)
    {
        if (_workspace.CurrentId != workspaceId) return;
        _ = _weeklyPlans.RefreshIfCachedAsync(year, weekNumber);
        // A plan change also affects coverage (which day-plans are covered by pantry).
        _ = _coverage.RefreshIfLoadedAsync();
    }

    private void OnMealsChanged(Guid workspaceId)
    {
        if (_workspace.CurrentId != workspaceId) return;
        _ = _meals.RefreshIfLoadedAsync();
    }

    private void OnWorkspaceChanged()
    {
        // Workspace switched (or signed out). All cached data belongs to the previous
        // workspace and must not bleed into the new one. Clearing fires OnChanged on
        // each cache so any open page will re-read (and re-fetch lazily) for the new ws.
        _pantry.Clear();
        _shopping.Clear();
        _weeklyPlans.Clear();
        _meals.Clear();
        _coverage.Clear();
    }

    public void Dispose()
    {
        if (!_started) return;
        _hub.PantryChanged -= OnPantryChanged;
        _hub.ShoppingListChanged -= OnShoppingChanged;
        _hub.WeeklyPlanChanged -= OnWeeklyPlanChanged;
        _hub.MealsChanged -= OnMealsChanged;
        _workspace.OnChange -= OnWorkspaceChanged;
        _started = false;
    }
}
