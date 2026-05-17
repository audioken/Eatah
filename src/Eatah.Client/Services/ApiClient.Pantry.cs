using System.Net.Http.Json;
using Eatah.Client.Services.Contracts;

namespace Eatah.Client.Services;

public partial class ApiClient
{
    // ---- Ingredient catalog ----

    public async Task<List<IngredientResponse>> SearchIngredientsAsync(string? query = null, CancellationToken ct = default)
    {
        var url = string.IsNullOrWhiteSpace(query)
            ? "api/ingredients"
            : $"api/ingredients?q={Uri.EscapeDataString(query)}";
        var response = await _http.GetAsync(url, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<List<IngredientResponse>>(cancellationToken: ct) ?? [];
    }

    public async Task<IngredientResponse?> CreateIngredientAsync(CreateIngredientRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/ingredients", request, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<IngredientResponse>(cancellationToken: ct);
    }

    // ---- Pantry ----

    public async Task<List<PantryItemResponse>> GetPantryAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/pantry", ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<List<PantryItemResponse>>(cancellationToken: ct) ?? [];
    }

    public async Task<PantryItemResponse?> AddToPantryAsync(Guid ingredientId, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/pantry", new AddPantryItemRequest(ingredientId), ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<PantryItemResponse>(cancellationToken: ct);
    }

    public async Task RemoveFromPantryAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/pantry/{id}", ct);
        await EnsureSuccessAsync(response, ct);
    }

    // ---- Shopping list ----

    public async Task<List<ShoppingItemResponse>> GetShoppingListAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/shoppinglist", ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<List<ShoppingItemResponse>>(cancellationToken: ct) ?? [];
    }

    public async Task<ShoppingItemResponse?> AddToShoppingListAsync(Guid ingredientId, string? notes = null, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/shoppinglist", new AddShoppingItemRequest(ingredientId, notes), ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<ShoppingItemResponse>(cancellationToken: ct);
    }

    public async Task ToggleShoppingItemAsync(Guid id, bool isChecked, CancellationToken ct = default)
    {
        var response = await _http.PatchAsJsonAsync($"api/shoppinglist/{id}", new ToggleShoppingItemRequest(isChecked), ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task RemoveFromShoppingListAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/shoppinglist/{id}", ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task ClearCheckedShoppingItemsAsync(CancellationToken ct = default)
    {
        var response = await _http.PostAsync("api/shoppinglist/clear-checked", null, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task<List<ShoppingItemResponse>> SyncShoppingFromWeeklyPlanAsync(Guid weeklyPlanId, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/shoppinglist/sync", new SyncWeeklyPlanRequest(weeklyPlanId), ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<List<ShoppingItemResponse>>(cancellationToken: ct) ?? [];
    }
}
