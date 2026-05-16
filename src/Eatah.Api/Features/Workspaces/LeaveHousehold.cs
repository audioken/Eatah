using Eatah.Api.Common;

namespace Eatah.Api.Features.Workspaces;

public static class LeaveHousehold
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
        var result = await service.LeaveHouseholdAsync(userId, ct);
        return result.ToNoContentResult();
    }
}
