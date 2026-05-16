using Eatah.Api.Common;

namespace Eatah.Api.Features.Notifications;

public static class GetMyNotifications
{
    public static async Task<IResult> Handle(
        bool? unreadOnly,
        INotificationService service,
        ICurrentUser currentUser,
        CancellationToken ct)
    {
        if (currentUser.UserId is not Guid userId)
            return Error.Unauthorized(ErrorCodes.AuthNotAuthenticated, "Not authenticated.").ToHttpResult();
        var list = await service.GetMineAsync(userId, unreadOnly ?? false, ct);
        return Results.Ok(list);
    }
}

public static class MarkNotificationAsRead
{
    public static async Task<IResult> Handle(
        Guid id,
        INotificationService service,
        ICurrentUser currentUser,
        CancellationToken ct)
    {
        if (currentUser.UserId is not Guid userId)
            return Error.Unauthorized(ErrorCodes.AuthNotAuthenticated, "Not authenticated.").ToHttpResult();
        var result = await service.MarkAsReadAsync(userId, id, ct);
        return result.ToNoContentResult();
    }
}

public static class MarkAllNotificationsAsRead
{
    public static async Task<IResult> Handle(
        INotificationService service,
        ICurrentUser currentUser,
        CancellationToken ct)
    {
        if (currentUser.UserId is not Guid userId)
            return Error.Unauthorized(ErrorCodes.AuthNotAuthenticated, "Not authenticated.").ToHttpResult();
        await service.MarkAllAsReadAsync(userId, ct);
        return Results.NoContent();
    }
}

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();
        group.MapGet("/", GetMyNotifications.Handle);
        group.MapPost("/{id:guid}/read", MarkNotificationAsRead.Handle);
        group.MapPost("/read-all", MarkAllNotificationsAsRead.Handle);
    }

    public static IServiceCollection AddNotificationFeature(this IServiceCollection services)
    {
        services.AddScoped<INotificationService, NotificationService>();
        return services;
    }
}
