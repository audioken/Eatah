namespace Eatah.Client.Services;

/// <summary>
/// Singleton that provides fire-and-forget shopping list sync from the current weekly plan.
/// Call TriggerSync() after any meal plan mutation so the shopping list stays accurate.
/// Items with Notes (plan-derived) that are no longer needed are removed automatically.
/// Manually added items (no Notes) are never touched.
/// </summary>
public class ShoppingSyncService
{
    private readonly ApiClient _api;
    private readonly ShoppingStateService _shoppingState;

    public ShoppingSyncService(ApiClient api, ShoppingStateService shoppingState)
    {
        _api = api;
        _shoppingState = shoppingState;
    }

    /// <summary>Fire-and-forget sync from the current week's plan. Silent on failure.</summary>
    public void TriggerSync()
    {
        _ = SyncAsync();
    }

    private async Task SyncAsync()
    {
        try
        {
            _shoppingState.SetShopping(await _api.SyncShoppingFromCurrentPlanAsync());
        }
        catch
        {
            // Silent failure – don't interrupt the user
        }
    }
}
