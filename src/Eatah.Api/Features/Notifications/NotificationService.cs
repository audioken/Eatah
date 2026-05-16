using System.Text.Json;
using Eatah.Api.Common;
using Eatah.Domain.Entities;
using Eatah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Eatah.Api.Features.Notifications;

public interface INotificationService
{
    Task NotifyAsync(Guid userId, NotificationType type, object payload, CancellationToken ct);
    Task<List<NotificationResponse>> GetMineAsync(Guid userId, bool unreadOnly, CancellationToken ct);
    Task<Result> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken ct);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken ct);
}

public class NotificationService : INotificationService
{
    private readonly EatahDbContext _db;
    public NotificationService(EatahDbContext db) => _db = db;

    public async Task NotifyAsync(Guid userId, NotificationType type, object payload, CancellationToken ct)
    {
        var n = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Payload = JsonSerializer.Serialize(payload),
            CreatedAt = DateTime.UtcNow
        };
        _db.Notifications.Add(n);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<NotificationResponse>> GetMineAsync(Guid userId, bool unreadOnly, CancellationToken ct)
    {
        var query = _db.Notifications.AsNoTracking().Where(n => n.UserId == userId);
        if (unreadOnly) query = query.Where(n => n.ReadAt == null);
        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationResponse(n.Id, n.Type, n.Payload, n.CreatedAt, n.ReadAt))
            .ToListAsync(ct);
    }

    public async Task<Result> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken ct)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == notificationId, ct);
        if (n is null) return Error.NotFound(ErrorCodes.NotificationNotFound, "Notification not found.");
        if (n.UserId != userId) return Error.Forbidden(ErrorCodes.NotificationAccessDenied, "Not your notification.");
        if (n.ReadAt is null)
        {
            n.ReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
        return Result.Success();
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        await _db.Notifications
            .Where(n => n.UserId == userId && n.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, now), ct);
    }
}
