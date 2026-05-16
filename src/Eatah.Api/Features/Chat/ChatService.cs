using Eatah.Api.Common;
using Eatah.Domain.Entities;
using Eatah.Infrastructure.Identity;
using Eatah.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Eatah.Api.Features.Chat;

public class ChatService
{
    private const int MaxMessageLength = 2000;
    private static readonly HashSet<string> AllowedEmojis = new() { "👍", "❤", "😂", "🎉", "🤔" };

    private readonly EatahDbContext _db;
    private readonly IWorkspaceContext _ws;
    private readonly UserManager<EatahUser> _users;

    public ChatService(EatahDbContext db, IWorkspaceContext ws, UserManager<EatahUser> users)
    {
        _db = db;
        _ws = ws;
        _users = users;
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

    public async Task<Result<List<ChatMessageResponse>>> GetMessagesAsync(Guid threadId, DateTime? before, int take, CancellationToken ct)
    {
        var wsId = _ws.RequireCurrent();
        var thread = await _db.ChatThreads.FirstOrDefaultAsync(t => t.Id == threadId && t.WorkspaceId == wsId, ct);
        if (thread is null) return Error.NotFound(ErrorCodes.ChatThreadNotFound, "Chat thread not found.");

        var query = _db.ChatMessages.AsNoTracking()
            .Include(m => m.Reactions)
            .Where(m => m.ThreadId == threadId);
        if (before is DateTime b) query = query.Where(m => m.CreatedAt < b);

        var rows = await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(ct);

        // Look up author display names (small set).
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
        return MapMessage(msg, authors);
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
        return MapMessage(msg, authors);
    }

    public async Task<Result> DeleteAsync(Guid messageId, Guid userId, CancellationToken ct)
    {
        var wsId = _ws.RequireCurrent();
        var msg = await _db.ChatMessages.Include(m => m.Thread).FirstOrDefaultAsync(m => m.Id == messageId, ct);
        if (msg is null || msg.Thread!.WorkspaceId != wsId)
            return Error.NotFound(ErrorCodes.ChatMessageNotFound, "Chat message not found.");
        if (msg.AuthorUserId != userId)
            return Error.Forbidden(ErrorCodes.ChatMessageNotOwned, "Not your message.");
        msg.DeletedAt = DateTime.UtcNow;
        msg.Text = "";
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ToggleReactionAsync(Guid messageId, string emoji, Guid userId, CancellationToken ct)
    {
        if (!AllowedEmojis.Contains(emoji))
            return Error.BadRequest(ErrorCodes.ChatReactionInvalid, "Unsupported emoji.");
        var wsId = _ws.RequireCurrent();
        var msg = await _db.ChatMessages.Include(m => m.Thread).FirstOrDefaultAsync(m => m.Id == messageId, ct);
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
        return Result.Success();
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
