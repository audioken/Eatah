using Eatah.Api.Common;

namespace Eatah.Api.Features.Friends;

public static class SearchUsers
{
    public static async Task<IResult> Handle(string? q, FriendService service, ICurrentUser currentUser, CancellationToken ct)
    {
        if (currentUser.UserId is not Guid userId)
            return Error.Unauthorized(ErrorCodes.AuthNotAuthenticated, "Not authenticated.").ToHttpResult();
        var results = await service.SearchUsersAsync(userId, q ?? "", ct);
        return Results.Ok(results);
    }
}

public static class SendFriendRequest
{
    public static async Task<IResult> Handle(SendFriendRequestRequest request, FriendService service, ICurrentUser currentUser, CancellationToken ct)
    {
        if (currentUser.UserId is not Guid userId)
            return Error.Unauthorized(ErrorCodes.AuthNotAuthenticated, "Not authenticated.").ToHttpResult();
        var result = await service.SendAsync(userId, request.ToUserId, ct);
        return result.ToHttpResult();
    }
}

public static class RespondToFriendRequest
{
    public static async Task<IResult> Handle(Guid id, RespondToFriendRequestRequest request, FriendService service, ICurrentUser currentUser, CancellationToken ct)
    {
        if (currentUser.UserId is not Guid userId)
            return Error.Unauthorized(ErrorCodes.AuthNotAuthenticated, "Not authenticated.").ToHttpResult();
        var result = await service.RespondAsync(userId, id, request.Accept, ct);
        return result.ToNoContentResult();
    }
}

public static class CancelFriendRequest
{
    public static async Task<IResult> Handle(Guid id, FriendService service, ICurrentUser currentUser, CancellationToken ct)
    {
        if (currentUser.UserId is not Guid userId)
            return Error.Unauthorized(ErrorCodes.AuthNotAuthenticated, "Not authenticated.").ToHttpResult();
        var result = await service.CancelAsync(userId, id, ct);
        return result.ToNoContentResult();
    }
}

public static class GetMyFriends
{
    public static async Task<IResult> Handle(FriendService service, ICurrentUser currentUser, CancellationToken ct)
    {
        if (currentUser.UserId is not Guid userId)
            return Error.Unauthorized(ErrorCodes.AuthNotAuthenticated, "Not authenticated.").ToHttpResult();
        var list = await service.GetFriendsAsync(userId, ct);
        return Results.Ok(list);
    }
}

public static class GetPendingFriendRequests
{
    public static async Task<IResult> Handle(FriendService service, ICurrentUser currentUser, CancellationToken ct)
    {
        if (currentUser.UserId is not Guid userId)
            return Error.Unauthorized(ErrorCodes.AuthNotAuthenticated, "Not authenticated.").ToHttpResult();
        var list = await service.GetPendingIncomingAsync(userId, ct);
        return Results.Ok(list);
    }
}

public static class FriendEndpoints
{
    public static void MapFriendEndpoints(this IEndpointRouteBuilder app)
    {
        var users = app.MapGroup("/api/users").WithTags("Users").RequireAuthorization();
        users.MapGet("/search", SearchUsers.Handle);

        var group = app.MapGroup("/api/friends").WithTags("Friends").RequireAuthorization();
        group.MapGet("/", GetMyFriends.Handle);
        group.MapGet("/requests/incoming", GetPendingFriendRequests.Handle);
        group.MapPost("/requests", SendFriendRequest.Handle);
        group.MapPost("/requests/{id:guid}/respond", RespondToFriendRequest.Handle);
        group.MapDelete("/requests/{id:guid}", CancelFriendRequest.Handle);
    }

    public static IServiceCollection AddFriendFeature(this IServiceCollection services)
    {
        services.AddScoped<FriendService>();
        return services;
    }
}
