using Eatah.Domain.Entities;

namespace Eatah.Api.Features.Meals;

public record IngredientDto(Guid Id, string Name);

public record MealResponse(
    Guid Id,
    string Name,
    MealCategory Category,
    DateTime CreatedAt,
    List<IngredientDto> Ingredients);

public record CreateMealRequest(
    string Name,
    MealCategory Category,
    List<string> Ingredients);

public record UpdateMealRequest(
    string Name,
    MealCategory Category,
    List<string> Ingredients);
