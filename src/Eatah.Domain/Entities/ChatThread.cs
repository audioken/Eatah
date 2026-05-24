namespace Eatah.Domain.Entities;

public enum ChatThreadType
{
    Group = 0,
    Direct = 1
}

/// <summary>
/// A chat thread. One Group thread per workspace (auto-provisioned).
/// Direct threads are private between two workspace members.
/// </summary>
public class ChatThread
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public ChatThreadType Type { get; set; } = ChatThreadType.Group;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<ChatMessage> Messages { get; set; } = [];
    public List<ChatThreadParticipant> Participants { get; set; } = [];
}

/// <summary>Stores the two participants of a Direct thread.</summary>
public class ChatThreadParticipant
{
    public Guid ThreadId { get; set; }
    public ChatThread? Thread { get; set; }
    public Guid UserId { get; set; }
}

public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid ThreadId { get; set; }
    public ChatThread? Thread { get; set; }
    public Guid AuthorUserId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EditedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public List<ChatReaction> Reactions { get; set; } = [];
}

public class ChatReaction
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public ChatMessage? Message { get; set; }
    public Guid UserId { get; set; }
    /// <summary>Single-character emoji (e.g. "👍", "❤", "😂").</summary>
    public string Emoji { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Tracks the last time a user read a chat thread, used to compute unread counts.</summary>
public class ChatThreadReadStatus
{
    public Guid UserId { get; set; }
    public Guid ThreadId { get; set; }
    public DateTime LastReadAt { get; set; } = DateTime.UtcNow;
}
