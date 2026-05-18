using Eatah.Api.Common;
using Eatah.Domain.Entities;
using Eatah.Infrastructure.Identity;
using Eatah.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Eatah.Api.Features.Chat;

public class ChatService
{
    private const int MaxMessageLength = 2000;
    private static readonly HashSet<string> AllowedEmojis = new() { "👍", "👎", "😂", "😢", "❤" };

    private readonly EatahDbContext _db;
    private readonly IWorkspaceContext _ws;
    private readonly UserManager<EatahUser> _users;
    private readonly IHubContext<ChatHub> _hub;

    public ChatService(EatahDbContext db, IWorkspaceContext ws, UserManager<EatahUser> users, IHubContext<ChatHub> hub)
    {
        _db = db;
        _ws = ws;
        _users = users;
        _hub = hub;
    }

    public async Task<ChatThreadResponse> GetOrCreateGroupThreadAsync(CancellationToken ct)
    {
        var wsId = _ws.RequireCurrent();
        var thread = await _db.ChatThreads.FirstOrDefaultAsync(t => t.WorkspaceId == wsId && t.Type == ChatThreadType.Group, ct);
        if (thread is null)
        {
            thread = new ChatThread { Id = Guid.NewGuid(), WorkspaceId = wsId, Type = ChatThreadType.Group };
            _db.ChatThreads.Add(thread);
            await _db.SaveChangesAsync(ct);
        }
        return new ChatThreadResponse(thread.Id, thread.WorkspaceId);
    }

    /// <summary>Returns all threads accessible to the user in the current workspace (group + direct).</summary>
    public async Task<List<ChatThreadSummaryResponse>> GetMyThreadsAsync(Guid userId, CancellationToken ct)
    {
        var wsId = _ws.RequireCurrent();

        // Fetch group thread (there is at most one per workspace)
        var groupThread = await _db.ChatThreads
            .AsNoTracking()
            .Where(t => t.WorkspaceId == wsId && t.Type == ChatThreadType.Group)
            .FirstOrDefaultAsync(ct);

        // Fetch direct threads in this workspace where the user is a participant
        var directThreads = await _db.ChatThreads
            .AsNoTracking()
            .Include(t => t.Participants)
            .Where(t => t.WorkspaceId == wsId && t.Type == ChatThreadType.Direct
                        && t.Participants.Any(p => p.UserId == userId))
            .ToListAsync(ct);

        // Collect all thread IDs to fetch last messages
        var allThreadIds = directThreads.Select(t => t.Id).ToList();
        if (groupThread is not null) allThreadIds.Add(groupThread.Id);

        // Last message per thread
        var lastMessages = await _db.ChatMessages.AsNoTracking()
            .Where(m => allThreadIds.Contains(m.ThreadId) && m.DeletedAt == null)
            .GroupBy(m => m.ThreadId)
            .Select(g => g.OrderByDescending(m => m.CreatedAt).First())
            .ToListAsync(ct);
        var lastMsgMap = lastMessages.ToDictionary(m => m.ThreadId);

        // Resolve display names for direct thread partners
        var partnerIds = directThreads
            .SelectMany(t => t.Participants.Where(p => p.UserId != userId).Select(p => p.UserId))
            .Distinct()
            .ToList();
        var partnerNames = await _users.Users.AsNoTracking()
            .Where(u => partnerIds.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName })
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct);

        var result = new List<ChatThreadSummaryResponse>();

        if (groupThread is not null)
        {
            lastMsgMap.TryGetValue(groupThread.Id, out var last);
            result.Add(new ChatThreadSummaryResponse(
                groupThread.Id, groupThread.WorkspaceId, "Group",
                null, null,
                last?.Text is { Length: > 0 } t ? (t.Length > 60 ? t[..60] + "…" : t) : null,
                last?.CreatedAt));
        }

        foreach (var dt in directThreads.OrderByDescending(t =>
            lastMsgMap.TryGetValue(t.Id, out var m) ? m.CreatedAt : t.CreatedAt))
        {
            var partner = dt.Participants.FirstOrDefault(p => p.UserId != userId);
            var partnerId = partner?.UserId;
            var partnerName = partnerId.HasValue && partnerNames.TryGetValue(partnerId.Value, out var n) ? n : "Okänd";
            lastMsgMap.TryGetValue(dt.Id, out var lastDm);
            result.Add(new ChatThreadSummaryResponse(
                dt.Id, dt.WorkspaceId, "Direct",
                partnerId, partnerName,
                lastDm?.Text is { Length: > 0 } t ? (t.Length > 60 ? t[..60] + "…" : t) : null,
                lastDm?.CreatedAt));
        }

        return result;
    }

    /// <summary>Gets or creates a direct thread between currentUserId and buddyUserId within the current workspace.</summary>
    public async Task<Result<ChatThreadSummaryResponse>> GetOrCreateDirectThreadAsync(Guid currentUserId, Guid buddyUserId, CancellationToken ct)
    {
        var wsId = _ws.RequireCurrent();

        // Both users must be workspace members.
        var memberIds = await _db.WorkspaceMembers
            .Where(m => m.WorkspaceId == wsId && (m.UserId == currentUserId || m.UserId == buddyUserId))
            .Select(m => m.UserId)
            .ToListAsync(ct);
        if (!memberIds.Contains(currentUserId) || !memberIds.Contains(buddyUserId))
            return Error.Forbidden(ErrorCodes.ChatNotBuddies, "Both users must be workspace members.");

        // Look for an existing direct thread between the two users in this workspace.
        var thread = await _db.ChatThreads
            .Include(t => t.Participants)
            .Where(t => t.WorkspaceId == wsId && t.Type == ChatThreadType.Direct
                        && t.Participants.Any(p => p.UserId == currentUserId)
                        && t.Participants.Any(p => p.UserId == buddyUserId))
            .FirstOrDefaultAsync(ct);

        if (thread is null)
        {
            thread = new ChatThread
            {
                Id = Guid.NewGuid(),
                WorkspaceId = wsId,
                Type = ChatThreadType.Direct,
                Participants =
                [
                    new ChatThreadParticipant { UserId = currentUserId },
                    new ChatThreadParticipant { UserId = buddyUserId }
                ]
            };
            _db.ChatThreads.Add(thread);
            await _db.SaveChangesAsync(ct);
        }

        var buddy = await _users.FindByIdAsync(buddyUserId.ToString());
        return new ChatThreadSummaryResponse(
            thread.Id, thread.WorkspaceId, "Direct",
            buddyUserId, buddy?.DisplayName ?? "Okänd",
            null, null);
    }

    public async Task<Result<List<ChatMessageResponse>>> GetMessagesAsync(Guid threadId, DateTime? before, int take, CancellationToken ct)
    {
        var wsId = _ws.RequireCurrent();
        var thread = await _db.ChatThreads
            .Include(t => t.Participants)
            .FirstOrDefaultAsync(t => t.Id == threadId && t.WorkspaceId == wsId, ct);
        if (thread is null) return Error.NotFound(ErrorCodes.ChatThreadNotFound, "Chat thread not found.");

        var query = _db.ChatMessages.AsNoTracking()
            .Include(m => m.Reactions)
            .Where(m => m.ThreadId == threadId);
        if (before is DateTime b) query = query.Where(m => m.CreatedAt < b);

        var rows = await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(ct);

        var authorIds = rows.Select(r => r.AuthorUserId).Distinct().ToList();
        var authors = await _users.Users.AsNoTracking()
            .Where(u => authorIds.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName })
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct);

        return rows.OrderBy(r => r.CreatedAt).Select(m => MapMessage(m, authors)).ToList();
    }

    public async Task<Result<ChatMessageResponse>> SendAsync(Guid threadId, string text, Guid userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text)) return Error.BadRequest(ErrorCodes.ValidationError, "Message text is required.");
        if (text.Length > MaxMessageLength) return Error.BadRequest(ErrorCodes.ChatMessageTooLong, "Message is too long.");
        var wsId = _ws.RequireCurrent();
        var thread = await _db.ChatThreads.FirstOrDefaultAsync(t => t.Id == threadId && t.WorkspaceId == wsId, ct);
        if (thread is null) return Error.NotFound(ErrorCodes.ChatThreadNotFound, "Chat thread not found.");

        var msg = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ThreadId = threadId,
            AuthorUserId = userId,
            Text = text.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        _db.ChatMessages.Add(msg);
        await _db.SaveChangesAsync(ct);

        var user = await _users.FindByIdAsync(userId.ToString());
        var authors = new Dictionary<Guid, string> { [userId] = user?.DisplayName ?? "" };
        var response = MapMessage(msg, authors);
        await _hub.Clients.Group($"thread:{threadId}").SendAsync("MessageReceived", response, ct);
        return response;
    }

    public async Task<Result<ChatMessageResponse>> EditAsync(Guid messageId, string text, Guid userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text)) return Error.BadRequest(ErrorCodes.ValidationError, "Message text is required.");
        if (text.Length > MaxMessageLength) return Error.BadRequest(ErrorCodes.ChatMessageTooLong, "Message is too long.");
        var wsId = _ws.RequireCurrent();
        var msg = await _db.ChatMessages
            .Include(m => m.Reactions)
            .Include(m => m.Thread)
            .FirstOrDefaultAsync(m => m.Id == messageId, ct);
        if (msg is null || msg.Thread!.WorkspaceId != wsId)
            return Error.NotFound(ErrorCodes.ChatMessageNotFound, "Chat message not found.");
        if (msg.AuthorUserId != userId)
            return Error.Forbidden(ErrorCodes.ChatMessageNotOwned, "Not your message.");

        msg.Text = text.Trim();
        msg.EditedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var user = await _users.FindByIdAsync(userId.ToString());
        var authors = new Dictionary<Guid, string> { [userId] = user?.DisplayName ?? "" };
        var response = MapMessage(msg, authors);
        await _hub.Clients.Group($"thread:{msg.ThreadId}").SendAsync("MessageEdited", response, ct);
        return response;
    }

    public async Task<Result> DeleteAsync(Guid messageId, Guid userId, CancellationToken ct)
    {
        var wsId = _ws.RequireCurrent();
        var msg = await _db.ChatMessages.Include(m => m.Thread).FirstOrDefaultAsync(m => m.Id == messageId, ct);
        if (msg is null || msg.Thread!.WorkspaceId != wsId)
            return Error.NotFound(ErrorCodes.ChatMessageNotFound, "Chat message not found.");
        if (msg.AuthorUserId != userId)
            return Error.Forbidden(ErrorCodes.ChatMessageNotOwned, "Not your message.");
        var threadId = msg.ThreadId;
        msg.DeletedAt = DateTime.UtcNow;
        msg.Text = "";
        await _db.SaveChangesAsync(ct);
        await _hub.Clients.Group($"thread:{threadId}").SendAsync("MessageDeleted", messageId, ct);
        return Result.Success();
    }

    public async Task<Result<List<ChatReactionGroupResponse>>> ToggleReactionAsync(Guid messageId, string emoji, Guid userId, CancellationToken ct)
    {
        if (!AllowedEmojis.Contains(emoji))
            return Error.BadRequest(ErrorCodes.ChatReactionInvalid, "Unsupported emoji.");
        var wsId = _ws.RequireCurrent();
        var msg = await _db.ChatMessages
            .Include(m => m.Thread)
            .Include(m => m.Reactions)
            .FirstOrDefaultAsync(m => m.Id == messageId, ct);
        if (msg is null || msg.Thread!.WorkspaceId != wsId)
            return Error.NotFound(ErrorCodes.ChatMessageNotFound, "Chat message not found.");

        var existing = await _db.ChatReactions.FirstOrDefaultAsync(
            r => r.MessageId == messageId && r.UserId == userId && r.Emoji == emoji, ct);
        if (existing is not null)
        {
            _db.ChatReactions.Remove(existing);
        }
        else
        {
            _db.ChatReactions.Add(new ChatReaction
            {
                Id = Guid.NewGuid(),
                MessageId = messageId,
                UserId = userId,
                Emoji = emoji,
                CreatedAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync(ct);

        // Re-read reactions after update to broadcast fresh state
        var updatedReactions = await _db.ChatReactions.AsNoTracking()
            .Where(r => r.MessageId == messageId)
            .ToListAsync(ct);
        var groups = updatedReactions
            .GroupBy(r => r.Emoji)
            .Select(g => new ChatReactionGroupResponse(g.Key, g.Count(), g.Select(r => r.UserId).ToList()))
            .ToList();
        await _hub.Clients.Group($"thread:{msg.ThreadId}").SendAsync("ReactionUpdated",
            new { messageId, reactions = groups }, ct);
        return Result<List<ChatReactionGroupResponse>>.Success(groups);
    }

    private static ChatMessageResponse MapMessage(ChatMessage m, IReadOnlyDictionary<Guid, string> authors)
    {
        var groups = m.Reactions
            .GroupBy(r => r.Emoji)
            .Select(g => new ChatReactionGroupResponse(g.Key, g.Count(), g.Select(r => r.UserId).ToList()))
            .ToList();
        return new ChatMessageResponse(
            m.Id, m.ThreadId, m.AuthorUserId,
            authors.TryGetValue(m.AuthorUserId, out var name) ? name : "",
            m.DeletedAt is null ? m.Text : "",
            m.CreatedAt, m.EditedAt, m.DeletedAt, groups);
    }
}
