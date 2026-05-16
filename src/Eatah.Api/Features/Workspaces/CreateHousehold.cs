using Eatah.Api.Common;

namespace Eatah.Api.Features.Workspaces;

public static class CreateHousehold
{
    public static async Task<IResult> Handle(
        CreateHouseholdRequest request,
        WorkspaceService service,
        ICurrentUser currentUser,
        CancellationToken ct)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return Error.Unauthorized(ErrorCodes.AuthNotAuthenticated, "Not authenticated.").ToHttpResult();
        }
        if (string.IsNullOrWhiteSpace(request?.Name))
        {
            return Error.BadRequest(ErrorCodes.ValidationError, "Name is required.").ToHttpResult();
        }
        var result = await service.CreateHouseholdAsync(userId, request.Name, ct);
        return result.ToHttpResult();
    }
}
