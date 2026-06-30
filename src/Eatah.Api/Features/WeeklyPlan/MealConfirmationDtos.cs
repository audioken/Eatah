namespace Eatah.Api.Features.WeeklyPlan;

public record PendingConfirmationResponse(
    Guid DayPlanId,
    string MealName,
    DayOfWeek DayOfWeek,
    int WeekNumber,
    int Year);

public record ConfirmMealItem(Guid DayPlanId, bool Eaten);

public record ConfirmMealsRequest(List<ConfirmMealItem> Confirmations);
