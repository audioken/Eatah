using System.Security.Claims;
using Eatah.Api.Common;
using Eatah.Api.Features.Workspaces;
using Eatah.Infrastructure.Identity;
using Eatah.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Eatah.Api.Features.Auth;

public static class DeleteAccount
{
    public static async Task<IResult> Handle(
        [FromBody] DeleteAccountRequest request,
        UserManager<EatahUser> userManager,
        ClaimsPrincipal principal,
        EatahDbContext db,
        WorkspaceService workspaces,
        CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null)
            return Error.Unauthorized(ErrorCodes.AuthNotAuthenticated, "Not authenticated.").ToHttpResult();

        var valid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!valid)
            return Error.BadRequest(ErrorCodes.AuthPasswordInvalid, "Incorrect password.").ToHttpResult();

        // Delete notifications
        await db.Notifications
            .Where(n => n.UserId == user.Id)
            .ExecuteDeleteAsync(ct);

        // Delete friend requests
        await db.FriendRequests
            .Where(r => r.FromUserId == user.Id || r.ToUserId == user.Id)
            .ExecuteDeleteAsync(ct);

        // Remove workspace memberships; cascade-delete workspaces that become empty
        // (includes all workspace-scoped data: weekly plans, shopping, pantry, chat, meals…).
        var membershipIds = await db.WorkspaceMembers
            .Where(m => m.UserId == user.Id)
            .Select(m => m.WorkspaceId)
            .ToListAsync(ct);

        await db.WorkspaceMembers
            .Where(m => m.UserId == user.Id)
            .ExecuteDeleteAsync(ct);

        foreach (var wsId in membershipIds)
        {
            var remaining = await db.WorkspaceMembers.CountAsync(m => m.WorkspaceId == wsId, ct);
            if (remaining == 0)
            {
                await workspaces.DeleteWorkspaceCascadeAsync(wsId, ct);
            }
        }

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return Error.Unexpected(ErrorCodes.Unexpected, "Could not delete account.").ToHttpResult();

        return Results.NoContent();
    }
}
