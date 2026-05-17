namespace Eatah.Api.Features.Chat;

public record ChatThreadResponse(Guid Id, Guid WorkspaceId);

/// <summary>Summary of a thread as shown in the chat thread list.</summary>
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
public record SendMessageRequest(string Text);
public record EditMessageRequest(string Text);
public record ToggleReactionRequest(string Emoji);
public record GetOrCreateDirectThreadRequest(Guid BuddyUserId);
