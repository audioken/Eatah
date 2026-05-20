using Eatah.Domain.Entities;

namespace Eatah.Api.Features.WeeklyPlan;

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
    List<DayPlanResponse> Days,
    Guid? DietProfileId);

public record CreateWeeklyPlanRequest(int Year, int WeekNumber);

public record AssignMealRequest(Guid MealId);

public record RandomizeWeeklyPlanRequest(Guid? ProfileId);

public record RandomizeDayRequest(Guid? ProfileId);

public record UpdateWeeklyPlanDietProfileRequest(Guid? ProfileId);
