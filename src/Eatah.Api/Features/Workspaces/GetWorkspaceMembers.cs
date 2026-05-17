using Eatah.Api.Common;
using Eatah.Infrastructure.Identity;
using Eatah.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Eatah.Api.Features.Workspaces;

public static class GetWorkspaceMembers
{
    public static async Task<IResult> Handle(
        EatahDbContext db,
        IWorkspaceContext ws,
        ICurrentUser currentUser,
        UserManager<EatahUser> users,
        CancellationToken ct)
    {
        if (currentUser.UserId is not Guid uid)
            return Error.Unauthorized(ErrorCodes.AuthNotAuthenticated, "Not authenticated.").ToHttpResult();

        var wsId = ws.RequireCurrent();

        var memberIds = await db.WorkspaceMembers
            .Where(m => m.WorkspaceId == wsId && m.UserId != uid)
            .Select(m => m.UserId)
            .ToListAsync(ct);

        var members = await users.Users
            .AsNoTracking()
            .Where(u => memberIds.Contains(u.Id))
            .Select(u => new WorkspaceMemberResponse(u.Id, u.DisplayName ?? u.Email ?? "Okänd"))
            .ToListAsync(ct);

        return Results.Ok(members);
    }
}
