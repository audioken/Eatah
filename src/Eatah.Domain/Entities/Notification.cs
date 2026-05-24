namespace Eatah.Domain.Entities;

public enum NotificationType
{
    FriendRequest = 0,
    FriendRequestAccepted = 1,
    ChatMessage = 2,
    ChatMention = 3,
    HouseholdMemberLeft = 4
}

public class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    /// <summary>Free-form JSON payload (e.g. requestId, fromDisplayName, threadId).</summary>
    public string Payload { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}
