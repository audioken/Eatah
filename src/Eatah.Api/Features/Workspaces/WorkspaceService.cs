using Eatah.Api.Common;
using Eatah.Api.Features.Chat;
using Eatah.Domain.Entities;
using Eatah.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Eatah.Api.Features.Workspaces;

public class WorkspaceService
{
    private readonly IWorkspaceRepository _repo;
    private readonly EatahDbContext _db;
    private readonly IHubContext<ChatHub> _hub;

    public WorkspaceService(IWorkspaceRepository repo, EatahDbContext db, IHubContext<ChatHub> hub)
    {
        _repo = repo;
        _db = db;
        _hub = hub;
    }

    public async Task<List<WorkspaceResponse>> GetForUserAsync(Guid userId, CancellationToken ct)
    {
        var list = await _repo.GetForUserAsync(userId, ct);
        return list.Select(w => ToResponse(w, userId)).ToList();
    }

    public async Task<Result<WorkspaceResponse>> CreateHouseholdAsync(Guid userId, string name, CancellationToken ct)
    {
        if (await _repo.HasAnyAsync(userId, ct))
        {
            return Error.Conflict(ErrorCodes.WorkspaceHouseholdAlreadyExists,
                "You already belong to a household.");
        }

        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Members =
            [
                new WorkspaceMember { UserId = userId, Role = MemberRole.Owner }
            ]
        };
        await _repo.AddAsync(workspace, ct);
        return ToResponse(workspace, userId);
    }

    public async Task<Result> LeaveHouseholdAsync(Guid userId, CancellationToken ct)
    {
        var membership = await _db.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.UserId == userId, ct);
        if (membership is null)
        {
            return Error.NotFound(ErrorCodes.WorkspaceNotFound, "You don't belong to any household.");
        }

        _db.WorkspaceMembers.Remove(membership);
        await _db.SaveChangesAsync(ct);

        // If the household has no remaining members, delete it (cascades all workspace-scoped data).
        var remaining = await _db.WorkspaceMembers.CountAsync(m => m.WorkspaceId == membership.WorkspaceId, ct);
        if (remaining == 0)
        {
            await DeleteWorkspaceCascadeAsync(membership.WorkspaceId, ct);
        }

        // Auto-create a new clean solo household so the user is never left workspace-less.
        await EnsureDefaultHouseholdAsync(userId, ct);

        return Result.Success();
    }

    public async Task<Result<WorkspaceResponse>> RenameAsync(
        Guid userId, Guid workspaceId, string name, CancellationToken ct)
    {
        var ws = await _repo.GetByIdAsync(workspaceId, ct);
        if (ws is null) return Error.NotFound(ErrorCodes.WorkspaceNotFound, "Household not found.");
        var member = ws.Members.FirstOrDefault(m => m.UserId == userId);
        if (member is null)
        {
            return Error.Forbidden(ErrorCodes.WorkspaceAccessDenied, "You are not a member of this household.");
        }
        ws.Name = name.Trim();
        await _repo.SaveChangesAsync(ct);

        // Broadcast so other members see the new name without reloading.
        await _hub.Clients.Group($"workspace:{ws.Id}")
            .SendAsync("WorkspaceRenamed", new { workspaceId = ws.Id, name = ws.Name }, ct);

        return ToResponse(ws, userId);
    }

    /// <summary>
    /// Ensures the user has a household. Idempotent — creates "Mitt hushåll" with the user as owner if missing.
    /// </summary>
    public async Task EnsureDefaultHouseholdAsync(Guid userId, CancellationToken ct)
    {
        if (await _repo.HasAnyAsync(userId, ct)) return;

        var household = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Mitt hushåll",
            Members =
            [
                new WorkspaceMember { UserId = userId, Role = MemberRole.Owner }
            ]
        };
        await _db.Workspaces.AddAsync(household, ct);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Deletes a workspace and all data scoped to it. Used both when the last member leaves
    /// and when an invitee joins another household (their existing household is replaced).
    /// </summary>
    public async Task DeleteWorkspaceCascadeAsync(Guid workspaceId, CancellationToken ct)
    {
        // Order matters: child tables before parents to satisfy FK constraints that aren't ON DELETE CASCADE.
        // (Workspace itself doesn't cascade to workspace-scoped tables; those FKs are pure data references.)

        // Shopping list
        await _db.ShoppingItems.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(ct);

        // Pantry (PantryItemMealCoverage cascades from PantryItem)
        await _db.PantryItems.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(ct);

        // Weekly plans (DayPlan cascades from WeeklyPlan)
        await _db.WeeklyPlans.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(ct);

        // Chat (messages, reactions, participants cascade from ChatThread)
        await _db.ChatThreads.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(ct);

        // Diet profiles (rules cascade) — only workspace-scoped ones; system profiles have WorkspaceId == null.
        await _db.DietProfiles.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(ct);

        // Meals (ingredients cascade) — workspace-scoped only
        await _db.Meals.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(ct);

        // User-created master ingredients in this workspace
        await _db.IngredientMaster.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(ct);

        // Friend requests pointing at this household (any status). Lets us drop the workspace cleanly.
        await _db.FriendRequests.Where(x => x.HouseholdWorkspaceId == workspaceId).ExecuteDeleteAsync(ct);

        // Finally the workspace itself (WorkspaceMembers cascade from Workspace).
        await _db.Workspaces.Where(x => x.Id == workspaceId).ExecuteDeleteAsync(ct);
    }

    internal static WorkspaceResponse ToResponse(Workspace w, Guid currentUserId)
    {
        var me = w.Members.FirstOrDefault(m => m.UserId == currentUserId);
        return new WorkspaceResponse(
            w.Id, w.Name, w.Members.Count, me?.Role == MemberRole.Owner);
    }
}
