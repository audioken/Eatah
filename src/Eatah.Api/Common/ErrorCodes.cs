namespace Eatah.Api.Common;

/// <summary>
/// Machine-readable error codes returned in ProblemDetails responses (extension
/// member <c>errorCode</c>). Clients should switch on these values to display
/// localized messages — never parse the human-readable <c>detail</c> text.
/// </summary>
public static class ErrorCodes
{
    // Generic
    public const string ValidationError = "VALIDATION_ERROR";
    public const string Unexpected = "UNEXPECTED_ERROR";

    // Meals
    public const string MealNotFound = "MEAL_NOT_FOUND";

    // WeeklyPlan
    public const string WeeklyPlanNotFound = "WEEKLY_PLAN_NOT_FOUND";
    public const string WeeklyPlanConflict = "WEEKLY_PLAN_CONFLICT";
    public const string DayPlanNotFound = "DAY_PLAN_NOT_FOUND";

    // DietRules
    public const string DietProfileNotFound = "DIET_PROFILE_NOT_FOUND";

    // AI
    public const string AiServiceFailure = "AI_SERVICE_FAILURE";
    public const string AiServiceNotConfigured = "AI_SERVICE_NOT_CONFIGURED";
    public const string AiInvalidResponse = "AI_INVALID_RESPONSE";
}
