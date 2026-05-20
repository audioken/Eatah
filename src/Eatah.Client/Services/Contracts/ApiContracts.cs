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

public record CreateDietRuleRequest(
    MealCategory Category,
    int MinPerWeek,
    int MaxPerWeek);

public record CreateDietProfileRequest(
    string Name,
    List<CreateDietRuleRequest> Rules);

// ---- Workspaces ----

public record WorkspaceResponse(Guid Id, string Name, int MemberCount, bool IsOwner);
public record RenameWorkspaceRequest(string Name);
public record CreateHouseholdRequest(string Name);
public record WorkspaceMemberResponse(Guid UserId, string DisplayName);

// ---- Friends ----

public enum RequestStatus { Pending = 0, Accepted = 1, Rejected = 2, Cancelled = 3 }

public record UserSearchResult(Guid Id, string DisplayName);
public record SendFriendRequestRequest(Guid ToUserId);
public record RespondToFriendRequestRequest(bool Accept);
public record FriendRequestResponse(
    Guid Id, Guid FromUserId, string FromDisplayName, Guid ToUserId, string ToDisplayName,
    Guid HouseholdWorkspaceId, RequestStatus Status, DateTime CreatedAt);
public record FriendResponse(Guid Id, string DisplayName);

// ---- Notifications ----

public enum NotificationType { FriendRequest = 0, FriendRequestAccepted = 1, ChatMessage = 2, ChatMention = 3 }

public record NotificationResponse(Guid Id, NotificationType Type, string Payload, DateTime CreatedAt, DateTime? ReadAt);

// ---- Profile ----

public record UpdateProfileRequest(string? DisplayName, string? Email);
public record DeleteAccountRequest(string Password);

// ---- Ingredients / Pantry / Shopping ----

public record IngredientResponse(Guid Id, string Name, string? Category, bool IsSystem);
public record CreateIngredientRequest(string Name, string? Category = null);
public record PantryItemResponse(Guid Id, Guid IngredientId, string Name, string? Category, DateTime AddedAt);
public record ShoppingItemResponse(Guid Id, Guid IngredientId, string Name, string? Category, bool IsChecked, DateTime AddedAt, string? Notes);
public record AddShoppingItemRequest(Guid IngredientId, string? Notes = null);
public record AddPantryItemRequest(Guid IngredientId);
public record ToggleShoppingItemRequest(bool IsChecked);
public record SyncWeeklyPlanRequest(Guid WeeklyPlanId);
public record PantryCoverageResponse(Guid IngredientId, Guid MealId, bool Covers);
public record SetPantryCoverageRequest(Guid IngredientId, Guid MealId, bool Covers);

// ---- Chat ----

public record ChatThreadSummaryResponse(
    Guid Id,
    Guid WorkspaceId,
    string Type,
    Guid? DirectPartnerId,
    string? DirectPartnerDisplayName,
    string? LastMessagePreview,
    DateTime? LastMessageAt);

public record ChatMessageResponse(
    Guid Id,
    Guid ThreadId,
    Guid AuthorUserId,
    string AuthorDisplayName,
    string Text,
    DateTime CreatedAt,
    DateTime? EditedAt,
    DateTime? DeletedAt,
    IReadOnlyList<ChatReactionGroupResponse> Reactions);

public record ChatReactionGroupResponse(string Emoji, int Count, IReadOnlyList<Guid> UserIds);
public record SendChatMessageRequest(string Text);
public record EditChatMessageRequest(string Text);
public record ToggleChatReactionRequest(string Emoji);
public record GetOrCreateDirectThreadRequest(Guid BuddyUserId);
public record ChatGroupThreadResponse(Guid Id, Guid WorkspaceId);
