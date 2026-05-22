namespace Eatah.Client.Services;

/// <summary>
/// Singleton that listens for workspace-scoped invalidation events from
/// <see cref="ChatHubService"/> and refetches the affected caches
/// (<see cref="PantryStateService"/>, <see cref="ShoppingStateService"/>)
/// when changes happen in the currently active workspace.
/// <para>
/// Started once at app boot via <see cref="Start"/>. Cross-workspace events
/// are ignored so we never refetch data for a household the user isn't viewing.
/// </para>
/// </summary>
public sealed class RealtimeSyncService : IDisposable
{
    private readonly ChatHubService _hub;
    private readonly ApiClient _api;
    private readonly WorkspaceState _workspace;
    private readonly PantryStateService _pantry;
    private readonly ShoppingStateService _shopping;
    private bool _started;

    public RealtimeSyncService(
        ChatHubService hub,
        ApiClient api,
        WorkspaceState workspace,
        PantryStateService pantry,
        ShoppingStateService shopping)
    {
        _hub = hub;
        _api = api;
        _workspace = workspace;
        _pantry = pantry;
        _shopping = shopping;
    }

    public void Start()
    {
        if (_started) return;
        _started = true;
        _hub.PantryChanged += OnPantryChanged;
        _hub.ShoppingListChanged += OnShoppingChanged;
    }

    private void OnPantryChanged(Guid workspaceId)
    {
        if (_workspace.CurrentId != workspaceId) return;
        _ = RefetchPantryAsync();
    }

    private void OnShoppingChanged(Guid workspaceId)
    {
        if (_workspace.CurrentId != workspaceId) return;
        _ = RefetchShoppingAsync();
    }

    private async Task RefetchPantryAsync()
    {
        try
        {
            var items = await _api.GetPantryAsync();
            _pantry.SetPantry(items);
        }
        catch
        {
            // Network/auth blips are non-fatal — caches stay as-is.
        }
    }

    private async Task RefetchShoppingAsync()
    {
        try
        {
            var items = await _api.GetShoppingListAsync();
            _shopping.SetShopping(items);
        }
        catch
        {
            // Network/auth blips are non-fatal — caches stay as-is.
        }
    }

    public void Dispose()
    {
        if (!_started) return;
        _hub.PantryChanged -= OnPantryChanged;
        _hub.ShoppingListChanged -= OnShoppingChanged;
        _started = false;
    }
}
