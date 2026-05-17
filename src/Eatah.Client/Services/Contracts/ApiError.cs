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
    public const string MealsInsufficient = "meals_insufficient";

    public const string WeeklyPlanNotFound = "weekly_plan_not_found";
    public const string WeeklyPlanConflict = "weekly_plan_conflict";
    public const string DayPlanNotFound = "day_plan_not_found";

    public const string DietProfileNotFound = "diet_profile_not_found";

    public const string AiServiceFailure = "ai_service_failure";
    public const string AiServiceNotConfigured = "ai_service_not_configured";
    public const string AiInvalidResponse = "ai_invalid_response";

    // Auth
    public const string AuthEmailTaken = "auth_email_taken";
    public const string AuthDisplayNameTaken = "auth_display_name_taken";
    public const string AuthEmailNotConfirmed = "auth_email_not_confirmed";
    public const string AuthInvalidCredentials = "auth_invalid_credentials";
    public const string AuthInvalidToken = "auth_invalid_token";
    public const string AuthUserNotFound = "auth_user_not_found";
    public const string AuthPasswordInvalid = "auth_password_invalid";
    public const string AuthNotAuthenticated = "auth_not_authenticated";
    public const string AuthEmailSendFailed = "auth_email_send_failed";

    // Workspaces
    public const string WorkspaceNotFound = "workspace_not_found";
    public const string WorkspaceAccessDenied = "workspace_access_denied";
    public const string WorkspaceHouseholdAlreadyExists = "workspace_household_already_exists";

    // Friends
    public const string FriendRequestNotFound = "friend_request_not_found";
    public const string FriendRequestAlreadyPending = "friend_request_already_pending";
    public const string FriendRequestSelf = "friend_request_self";
    public const string FriendRequestCannotInviteHouseholdMember = "friend_request_cannot_invite_household_member";

    // Notifications
    public const string NotificationNotFound = "notification_not_found";

    // Ingredients / Pantry / Shopping
    public const string IngredientNotFound = "ingredient_not_found";
    public const string PantryItemNotFound = "pantry_item_not_found";
    public const string PantryItemAlreadyExists = "pantry_item_already_exists";
    public const string ShoppingItemNotFound = "shopping_item_not_found";

    // Chat
    public const string ChatThreadNotFound = "chat_thread_not_found";
    public const string ChatThreadAccessDenied = "chat_thread_access_denied";
    public const string ChatMessageNotFound = "chat_message_not_found";
    public const string ChatMessageNotOwned = "chat_message_not_owned";
    public const string ChatMessageTooLong = "chat_message_too_long";
    public const string ChatReactionInvalid = "chat_reaction_invalid";
    public const string ChatNotBuddies = "chat_not_buddies";
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
