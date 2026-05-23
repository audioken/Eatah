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
    /// <summary>Returned (HTTP 409) when an optimistic concurrency check fails — another workspace member modified the resource between read and write.</summary>
    public const string ConcurrencyConflict = "concurrency_conflict";

    // Meals
    public const string MealNotFound = "MEAL_NOT_FOUND";
    public const string MealsInsufficient = "meals_insufficient";

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

    // Auth (snake_case per plan; mirrored client-side in ApiErrorCodes)
    public const string AuthEmailTaken = "auth_email_taken";
    public const string AuthDisplayNameTaken = "auth_display_name_taken";
    public const string AuthEmailNotConfirmed = "auth_email_not_confirmed";
    public const string AuthInvalidCredentials = "auth_invalid_credentials";
    public const string AuthInvalidToken = "auth_invalid_token";
    public const string AuthUserNotFound = "auth_user_not_found";
    public const string AuthPasswordInvalid = "auth_password_invalid";
    public const string AuthNotAuthenticated = "auth_not_authenticated";
    public const string AuthEmailSendFailed = "auth_email_send_failed";
    public const string AuthPasswordRequiredForDestructiveAction = "auth_password_required_for_destructive_action";
    public const string AuthAccountDeleteConfirmationInvalid = "auth_account_delete_confirmation_invalid";
    public const string AuthEmailChangePending = "auth_email_change_pending";

    // Workspaces
    public const string WorkspaceNotFound = "workspace_not_found";
    public const string WorkspaceAccessDenied = "workspace_access_denied";
    public const string WorkspaceHouseholdAlreadyExists = "workspace_household_already_exists";
    public const string WorkspaceNotResolved = "workspace_not_resolved";

    // Friends / Notifications
    public const string FriendRequestNotFound = "friend_request_not_found";
    public const string FriendRequestAlreadyPending = "friend_request_already_pending";
    public const string FriendRequestSelf = "friend_request_self";
    public const string FriendRequestCannotInviteHouseholdMember = "friend_request_cannot_invite_household_member";
    public const string NotificationNotFound = "notification_not_found";
    public const string NotificationAccessDenied = "notification_access_denied";

    // Ingredients / Pantry / Shopping
    public const string IngredientNotFound = "ingredient_not_found";
    public const string IngredientSystemProtected = "ingredient_system_protected";
    public const string IngredientInUse = "ingredient_in_use";
    public const string IngredientNameRequired = "ingredient_name_required";
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

    // Push notifications
    public const string PushNotConfigured = "push_not_configured";
}
