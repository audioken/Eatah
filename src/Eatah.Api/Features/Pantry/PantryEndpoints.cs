using Eatah.Api.Common;

namespace Eatah.Api.Features.Pantry;

public static class PantryEndpoints
{
    public static void MapPantryEndpoints(this IEndpointRouteBuilder app)
    {
        var ing = app.MapGroup("/api/ingredients").WithTags("Ingredients").RequireAuthorization();
        ing.MapGet("/", async (string? q, IngredientCatalogService svc, CancellationToken ct)
            => Results.Ok(await svc.SearchAsync(q, ct)));
        ing.MapPost("/", async (CreateIngredientRequest req, IngredientCatalogService svc, CancellationToken ct)
            => (await svc.CreateAsync(req.Name, req.Category, ct)).ToHttpResult());

        var pantry = app.MapGroup("/api/pantry").WithTags("Pantry").RequireAuthorization();
        pantry.MapGet("/", async (PantryService svc, CancellationToken ct) => Results.Ok(await svc.GetAllAsync(ct)));
        pantry.MapPost("/", async (AddPantryItemRequest req, PantryService svc, CancellationToken ct)
            => (await svc.AddAsync(req.IngredientId, ct)).ToHttpResult());
        pantry.MapDelete("/{id:guid}", async (Guid id, PantryService svc, CancellationToken ct)
            => (await svc.RemoveAsync(id, ct)).ToNoContentResult());

        var shop = app.MapGroup("/api/shoppinglist").WithTags("ShoppingList").RequireAuthorization();
        shop.MapGet("/", async (ShoppingListService svc, CancellationToken ct) => Results.Ok(await svc.GetAllAsync(ct)));
        shop.MapPost("/", async (AddShoppingItemRequest req, ShoppingListService svc, CancellationToken ct)
            => (await svc.AddAsync(req.IngredientId, req.Notes, ct)).ToHttpResult());
        shop.MapPatch("/{id:guid}", async (Guid id, ToggleShoppingItemRequest req, ShoppingListService svc, CancellationToken ct)
            => (await svc.ToggleAsync(id, req.IsChecked, ct)).ToNoContentResult());
        shop.MapDelete("/{id:guid}", async (Guid id, ShoppingListService svc, CancellationToken ct)
            => (await svc.RemoveAsync(id, ct)).ToNoContentResult());
        shop.MapPost("/clear-checked", async (ShoppingListService svc, CancellationToken ct) =>
        {
            await svc.ClearCheckedAsync(ct);
            return Results.NoContent();
        });
        shop.MapPost("/sync", async (SyncWeeklyPlanRequest req, ShoppingListService svc, CancellationToken ct)
            => (await svc.SyncFromWeeklyPlanAsync(req.WeeklyPlanId, ct)).ToHttpResult());
        shop.MapPost("/sync/current", async (ShoppingListService svc, CancellationToken ct)
            => (await svc.SyncFromCurrentWeeklyPlanAsync(ct)).ToHttpResult());
    }

    public static IServiceCollection AddPantryFeature(this IServiceCollection services)
    {
        services.AddScoped<IngredientCatalogService>();
        services.AddScoped<PantryService>();
        services.AddScoped<ShoppingListService>();
        return services;
    }
}
