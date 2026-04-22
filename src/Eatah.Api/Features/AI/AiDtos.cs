using Eatah.Domain.Entities;

namespace Eatah.Api.Features.AI;

public record GenerateDietProfileRequest(
    string Name,
    string? Description);

public record AiGeneratedRule(
    MealCategory Category,
    int MinPerWeek,
    int MaxPerWeek,
    string Description);

public record AiGeneratedProfile(
    string Name,
    List<AiGeneratedRule> Rules);

public record GenerateMealRequest(
    MealCategory? Category,
    Guid? DietProfileId,
    Guid? WeeklyPlanId,
    DayOfWeek? TargetDay);

public record AiGeneratedMealResponse(
    string Name,
    MealCategory Category,
    int? CookingTimeMinutes,
    List<string> Ingredients);
