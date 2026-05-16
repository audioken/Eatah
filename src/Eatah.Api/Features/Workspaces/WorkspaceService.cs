using Eatah.Api.Common;
using Eatah.Domain.Entities;
using Eatah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Eatah.Api.Features.Workspaces;

public class WorkspaceService
{
    private readonly IWorkspaceRepository _repo;
    private readonly EatahDbContext _db;

    public WorkspaceService(IWorkspaceRepository repo, EatahDbContext db)
    {
        _repo = repo;
        _db = db;
    }

    public async Task<List<WorkspaceResponse>> GetForUserAsync(Guid userId, CancellationToken ct)
    {
        var list = await _repo.GetForUserAsync(userId, ct);
        return list.Select(w => ToResponse(w, userId)).ToList();
    }

    public async Task<Result<WorkspaceResponse>> CreateHouseholdAsync(Guid userId, string name, CancellationToken ct)
    {
        if (await _repo.HasHouseholdAsync(userId, ct))
        {
            return Error.Conflict(ErrorCodes.WorkspaceHouseholdAlreadyExists,
                "You already belong to a household workspace.");
        }

        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Type = WorkspaceType.Household,
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
            .Include(m => m.Workspace).ThenInclude(w => w.Members)
            .FirstOrDefaultAsync(m => m.UserId == userId && m.Workspace.Type == WorkspaceType.Household, ct);
        if (membership is null)
        {
            return Error.NotFound(ErrorCodes.WorkspaceNotFound, "You don't belong to any household workspace.");
        }

        _db.WorkspaceMembers.Remove(membership);
        await _db.SaveChangesAsync(ct);

        // Cascade-delete the household if it has no remaining members.
        var remaining = await _db.WorkspaceMembers.CountAsync(m => m.WorkspaceId == membership.WorkspaceId, ct);
        if (remaining == 0)
        {
            var ws = await _db.Workspaces.FirstAsync(w => w.Id == membership.WorkspaceId, ct);
            _db.Workspaces.Remove(ws);
            await _db.SaveChangesAsync(ct);
        }
        return Result.Success();
    }

    public async Task<Result<WorkspaceResponse>> RenameAsync(
        Guid userId, Guid workspaceId, string name, CancellationToken ct)
    {
        var ws = await _repo.GetByIdAsync(workspaceId, ct);
        if (ws is null) return Error.NotFound(ErrorCodes.WorkspaceNotFound, "Workspace not found.");
        var member = ws.Members.FirstOrDefault(m => m.UserId == userId);
        if (member is null)
        {
            return Error.Forbidden(ErrorCodes.WorkspaceAccessDenied, "You are not a member of this workspace.");
        }
        if (ws.Type == WorkspaceType.Personal)
        {
            return Error.Conflict(ErrorCodes.WorkspacePersonalProtected, "Personal workspaces cannot be renamed.");
        }
        ws.Name = name.Trim();
        await _repo.SaveChangesAsync(ct);
        return ToResponse(ws, userId);
    }

    /// <summary>
    /// Creates the Personal workspace for a freshly confirmed user. Idempotent.
    /// </summary>
    public async Task EnsurePersonalAsync(Guid userId, string displayName, CancellationToken ct)
    {
        var existing = await _db.WorkspaceMembers
            .AnyAsync(m => m.UserId == userId && m.Workspace.Type == WorkspaceType.Personal, ct);
        if (existing) return;

        var personal = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Personligt",
            Type = WorkspaceType.Personal,
            Members =
            [
                new WorkspaceMember { UserId = userId, Role = MemberRole.Owner }
            ]
        };
        await _db.Workspaces.AddAsync(personal, ct);
        await _db.SaveChangesAsync(ct);
    }

    internal static WorkspaceResponse ToResponse(Workspace w, Guid currentUserId)
    {
        var me = w.Members.FirstOrDefault(m => m.UserId == currentUserId);
        return new WorkspaceResponse(
            w.Id, w.Name, w.Type, w.Members.Count, me?.Role == MemberRole.Owner);
    }
}
