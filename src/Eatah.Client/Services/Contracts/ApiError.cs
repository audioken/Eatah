using System.Text.Json.Serialization;

namespace Eatah.Client.Services.Contracts;

/// <summary>
/// Standardized error payload returned by the API (RFC 7807 ProblemDetails + errorCode extension).
/// </summary>
public record ApiErrorResponse(
    int Status,
    string? Title,
    string? Detail,
    [property: JsonPropertyName("errorCode")] string? ErrorCode,
    IDictionary<string, string[]>? Errors);

/// <summary>
/// Well-known error codes returned by the API. Mirrors <c>Eatah.Api.Common.ErrorCodes</c>.
/// Use these when mapping API errors to user-facing messages.
/// </summary>
public static class ApiErrorCodes
{
    public const string ValidationError = "validation_error";
    public const string Unexpected = "unexpected_error";

    public const string MealNotFound = "meal_not_found";

    public const string WeeklyPlanNotFound = "weekly_plan_not_found";
    public const string WeeklyPlanConflict = "weekly_plan_conflict";
    public const string DayPlanNotFound = "day_plan_not_found";

    public const string DietProfileNotFound = "diet_profile_not_found";

    public const string AiServiceFailure = "ai_service_failure";
    public const string AiServiceNotConfigured = "ai_service_not_configured";
    public const string AiInvalidResponse = "ai_invalid_response";
}

/// <summary>
/// Thrown by <see cref="ApiClient"/> when the API responds with a non-success status code.
/// Carries the structured <see cref="ApiErrorResponse"/> so the UI can react to <c>ErrorCode</c>.
/// </summary>
public class ApiException : Exception
{
    public ApiErrorResponse Error { get; }

    public int Status => Error.Status;
    public string? ErrorCode => Error.ErrorCode;

    public ApiException(ApiErrorResponse error)
        : base(error.Detail ?? error.Title ?? $"API request failed with status {error.Status}.")
    {
        Error = error;
    }
}
