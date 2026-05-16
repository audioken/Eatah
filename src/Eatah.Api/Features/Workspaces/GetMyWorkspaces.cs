using Eatah.Api.Common;

namespace Eatah.Api.Features.Workspaces;

public static class GetMyWorkspaces
{
    public static async Task<IResult> Handle(
        WorkspaceService service,
        ICurrentUser currentUser,
        CancellationToken ct)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return Error.Unauthorized(ErrorCodes.AuthNotAuthenticated, "Not authenticated.").ToHttpResult();
        }
        var workspaces = await service.GetForUserAsync(userId, ct);
        return Results.Ok(workspaces);
    }
}
