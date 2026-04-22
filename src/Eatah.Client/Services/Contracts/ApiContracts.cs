namespace Eatah.Client.Services.Contracts;

public enum MealCategory
{
    Meat,
    Poultry,
    Fish,
    Vegetarian,
    Vegan
}

public record IngredientDto(Guid Id, string Name);

public record MealResponse(
    Guid Id,
    string Name,
    MealCategory Category,
    int? CookingTimeMinutes,
    DateTime CreatedAt,
    List<IngredientDto> Ingredients);

public record MealSummaryResponse(Guid Id, string Name, MealCategory Category);

public record DayPlanResponse(
    Guid Id,
    DayOfWeek DayOfWeek,
    Guid? MealId,
    MealSummaryResponse? Meal);

public record WeeklyPlanResponse(
    Guid Id,
    int Year,
    int WeekNumber,
    DateTime CreatedAt,
    List<DayPlanResponse> Days);

public record AssignMealRequest(Guid MealId);

public record CreateMealRequest(string Name, MealCategory Category, List<string> Ingredients, int? CookingTimeMinutes);

public record UpdateMealRequest(string Name, MealCategory Category, List<string> Ingredients, int? CookingTimeMinutes);

public record RandomizeWeeklyPlanRequest(Guid? ProfileId);

public record RandomizeDayRequest(Guid? ProfileId);

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

public record DietRuleResponse(
    Guid Id,
    MealCategory Category,
    int MinPerWeek,
    int MaxPerWeek,
    string Description);

public record DietProfileResponse(
    Guid Id,
    string Name,
    List<DietRuleResponse> Rules);

public record RuleResultResponse(
    string RuleName,
    MealCategory Category,
    bool IsMet,
    int Actual,
    int Min,
    int Max,
    double Score,
    string Message);

public record DietEvaluationResponse(
    double OverallScore,
    List<RuleResultResponse> RuleResults);

public record GenerateDietProfileRequest(
    string Name,
    string? Description);
