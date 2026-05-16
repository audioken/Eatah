using Eatah.Domain.Entities;

namespace Eatah.Api.Features.Notifications;

public record NotificationResponse(
    Guid Id,
    NotificationType Type,
    string Payload,
    DateTime CreatedAt,
    DateTime? ReadAt);
