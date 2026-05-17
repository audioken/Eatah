using Eatah.Api.Common;

namespace Eatah.Api.Features.Chat;

public static class ChatEndpoints
{
    public static void MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/chat").WithTags("Chat").RequireAuthorization();

        // List all threads the current user has access to in the current workspace
        group.MapGet("/threads", async (ChatService svc, ICurrentUser u, CancellationToken ct) =>
        {
            if (u.UserId is not Guid uid)
                return Error.Unauthorized(ErrorCodes.AuthNotAuthenticated, "Not authenticated.").ToHttpResult();
            return Results.Ok(await svc.GetMyThreadsAsync(uid, ct));
        });

        // GET current workspace's group thread (auto-create if missing)
        group.MapGet("/thread", async (ChatService svc, CancellationToken ct)
            => Results.Ok(await svc.GetOrCreateGroupThreadAsync(ct)));

        // Get or create a direct thread with a buddy in the current workspace
        group.MapPost("/threads/direct", async (GetOrCreateDirectThreadRequest req, ChatService svc, ICurrentUser u, CancellationToken ct) =>
        {
            if (u.UserId is not Guid uid)
                return Error.Unauthorized(ErrorCodes.AuthNotAuthenticated, "Not authenticated.").ToHttpResult();
            var result = await svc.GetOrCreateDirectThreadAsync(uid, req.BuddyUserId, ct);
            return result.ToHttpResult();
        });

        group.MapGet("/threads/{threadId:guid}/messages",
            async (Guid threadId, DateTime? before, int? take, ChatService svc, CancellationToken ct)
                => (await svc.GetMessagesAsync(threadId, before, take ?? 50, ct)).ToHttpResult());

        group.MapPost("/threads/{threadId:guid}/messages",
            async (Guid threadId, SendMessageRequest req, ChatService svc, ICurrentUser u, CancellationToken ct) =>
            {
                if (u.UserId is not Guid uid)
                    return Error.Unauthorized(ErrorCodes.AuthNotAuthenticated, "Not authenticated.").ToHttpResult();
                var result = await svc.SendAsync(threadId, req.Text, uid, ct);
                return result.ToHttpResult();
            });

        group.MapPatch("/messages/{messageId:guid}",
            async (Guid messageId, EditMessageRequest req, ChatService svc, ICurrentUser u, CancellationToken ct) =>
            {
                if (u.UserId is not Guid uid)
                    return Error.Unauthorized(ErrorCodes.AuthNotAuthenticated, "Not authenticated.").ToHttpResult();
                return (await svc.EditAsync(messageId, req.Text, uid, ct)).ToHttpResult();
            });

        group.MapDelete("/messages/{messageId:guid}",
            async (Guid messageId, ChatService svc, ICurrentUser u, CancellationToken ct) =>
            {
                if (u.UserId is not Guid uid)
                    return Error.Unauthorized(ErrorCodes.AuthNotAuthenticated, "Not authenticated.").ToHttpResult();
                return (await svc.DeleteAsync(messageId, uid, ct)).ToNoContentResult();
            });

        group.MapPost("/messages/{messageId:guid}/reactions",
            async (Guid messageId, ToggleReactionRequest req, ChatService svc, ICurrentUser u, CancellationToken ct) =>
            {
                if (u.UserId is not Guid uid)
                    return Error.Unauthorized(ErrorCodes.AuthNotAuthenticated, "Not authenticated.").ToHttpResult();
                return (await svc.ToggleReactionAsync(messageId, req.Emoji, uid, ct)).ToNoContentResult();
            });
    }

    public static IServiceCollection AddChatFeature(this IServiceCollection services)
    {
        services.AddScoped<ChatService>();
        return services;
    }
}
