namespace Eatah.Api.Features.Pantry;

public record IngredientResponse(Guid Id, string Name, string? Category, bool IsSystem);
public record CreateIngredientRequest(string Name, string? Category);
public record PantryItemResponse(Guid Id, Guid IngredientId, string Name, string? Category, DateTime AddedAt);
public record AddPantryItemRequest(Guid IngredientId);
public record ShoppingItemResponse(Guid Id, Guid IngredientId, string Name, string? Category, bool IsChecked, DateTime AddedAt, string? Notes);
public record AddShoppingItemRequest(Guid IngredientId, string? Notes = null);
public record ToggleShoppingItemRequest(bool IsChecked);
public record SyncWeeklyPlanRequest(Guid WeeklyPlanId);

public record PantryCoverageResponse(Guid IngredientId, Guid MealId, bool Covers);
public record SetPantryCoverageRequest(Guid IngredientId, Guid MealId, bool Covers);
