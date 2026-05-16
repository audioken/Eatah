namespace Eatah.Api.Features.Chat;

public record ChatThreadResponse(Guid Id, Guid WorkspaceId);
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
