using Eatah.Api.Common;
using Eatah.Api.Features.Notifications;
using Eatah.Domain.Entities;
using Eatah.Infrastructure.Identity;
using Eatah.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Eatah.Api.Features.Friends;

public class FriendService
{
    private readonly EatahDbContext _db;
    private readonly UserManager<EatahUser> _users;
    private readonly INotificationService _notifications;

    public FriendService(EatahDbContext db, UserManager<EatahUser> users, INotificationService notifications)
    {
        _db = db;
        _users = users;
        _notifications = notifications;
    }

    public async Task<List<UserSearchResult>> SearchUsersAsync(Guid currentUserId, string query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
            return new List<UserSearchResult>();
        var q = query.Trim();
        return await _users.Users
            .AsNoTracking()
            .Where(u => u.Id != currentUserId && u.DisplayName.ToLower().Contains(q.ToLower()))
            .OrderBy(u => u.DisplayName)
            .Take(10)
            .Select(u => new UserSearchResult(u.Id, u.DisplayName))
            .ToListAsync(ct);
    }

    public async Task<Result<FriendRequestResponse>> SendAsync(Guid fromUserId, Guid toUserId, CancellationToken ct)
    {
        if (fromUserId == toUserId)
            return Error.BadRequest(ErrorCodes.FriendRequestSelf, "You cannot invite yourself.");

        var toUser = await _users.FindByIdAsync(toUserId.ToString());
        if (toUser is null) return Error.NotFound(ErrorCodes.AuthUserNotFound, "User not found.");

        // Find or auto-create the sender's Household workspace.
        var household = await _db.WorkspaceMembers
            .Include(m => m.Workspace)
            .Where(m => m.UserId == fromUserId && m.Workspace.Type == WorkspaceType.Household)
            .Select(m => m.Workspace)
            .FirstOrDefaultAsync(ct);

        if (household is null)
        {
            household = new Workspace
            {
                Id = Guid.NewGuid(),
                Name = "Hushåll",
                Type = WorkspaceType.Household,
                Members = [new WorkspaceMember { UserId = fromUserId, Role = MemberRole.Owner }]
            };
            _db.Workspaces.Add(household);
            await _db.SaveChangesAsync(ct);
        }

        // Recipient must not already be a member of THIS household.
        var alreadyMember = await _db.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == household.Id && m.UserId == toUserId, ct);
        if (alreadyMember)
            return Error.Conflict(ErrorCodes.FriendRequestCannotInviteHouseholdMember,
                "User is already a member of your household.");

        // Block duplicate pending request.
        var existing = await _db.FriendRequests
            .AnyAsync(r => r.FromUserId == fromUserId && r.ToUserId == toUserId
                && r.HouseholdWorkspaceId == household.Id && r.Status == RequestStatus.Pending, ct);
        if (existing)
            return Error.Conflict(ErrorCodes.FriendRequestAlreadyPending, "A pending invitation already exists.");

        var request = new FriendRequest
        {
            Id = Guid.NewGuid(),
            FromUserId = fromUserId,
            ToUserId = toUserId,
            HouseholdWorkspaceId = household.Id,
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _db.FriendRequests.Add(request);
        await _db.SaveChangesAsync(ct);

        var fromUser = await _users.FindByIdAsync(fromUserId.ToString());
        await _notifications.NotifyAsync(toUserId, NotificationType.FriendRequest, new
        {
            requestId = request.Id,
            fromUserId,
            fromDisplayName = fromUser?.DisplayName,
            householdName = household.Name
        }, ct);

        return new FriendRequestResponse(
            request.Id, fromUserId, fromUser?.DisplayName ?? "", toUserId, toUser.DisplayName,
            household.Id, request.Status, request.CreatedAt);
    }

    public async Task<Result> RespondAsync(Guid currentUserId, Guid requestId, bool accept, CancellationToken ct)
    {
        var request = await _db.FriendRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct);
        if (request is null) return Error.NotFound(ErrorCodes.FriendRequestNotFound, "Friend request not found.");
        if (request.ToUserId != currentUserId)
            return Error.Forbidden(ErrorCodes.FriendRequestNotFound, "This invitation is not yours.");
        if (request.Status != RequestStatus.Pending)
            return Error.Conflict(ErrorCodes.FriendRequestNotFound, "Invitation already resolved.");

        if (accept)
        {
            var hasHousehold = await _db.WorkspaceMembers
                .AnyAsync(m => m.UserId == currentUserId && m.Workspace.Type == WorkspaceType.Household, ct);
            if (hasHousehold)
                return Error.Conflict(ErrorCodes.WorkspaceHouseholdAlreadyExists,
                    "You already belong to a household.");

            _db.WorkspaceMembers.Add(new WorkspaceMember
            {
                WorkspaceId = request.HouseholdWorkspaceId,
                UserId = currentUserId,
                Role = MemberRole.Member,
                JoinedAt = DateTime.UtcNow
            });
            request.Status = RequestStatus.Accepted;
        }
        else
        {
            request.Status = RequestStatus.Rejected;
        }

        request.RespondedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        if (accept)
        {
            var me = await _users.FindByIdAsync(currentUserId.ToString());
            await _notifications.NotifyAsync(request.FromUserId, NotificationType.FriendRequestAccepted, new
            {
                requestId = request.Id,
                byUserId = currentUserId,
                byDisplayName = me?.DisplayName
            }, ct);
        }
        return Result.Success();
    }

    public async Task<Result> CancelAsync(Guid currentUserId, Guid requestId, CancellationToken ct)
    {
        var request = await _db.FriendRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct);
        if (request is null) return Error.NotFound(ErrorCodes.FriendRequestNotFound, "Friend request not found.");
        if (request.FromUserId != currentUserId)
            return Error.Forbidden(ErrorCodes.FriendRequestNotFound, "Not your invitation.");
        if (request.Status != RequestStatus.Pending)
            return Error.Conflict(ErrorCodes.FriendRequestNotFound, "Invitation already resolved.");

        request.Status = RequestStatus.Cancelled;
        request.RespondedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<List<FriendResponse>> GetFriendsAsync(Guid currentUserId, CancellationToken ct)
    {
        // Foodbuddies = other members of any Household I'm in.
        var householdIds = await _db.WorkspaceMembers
            .Where(m => m.UserId == currentUserId && m.Workspace.Type == WorkspaceType.Household)
            .Select(m => m.WorkspaceId)
            .ToListAsync(ct);

        if (householdIds.Count == 0) return new();

        var memberIds = await _db.WorkspaceMembers
            .Where(m => householdIds.Contains(m.WorkspaceId) && m.UserId != currentUserId)
            .Select(m => m.UserId)
            .Distinct()
            .ToListAsync(ct);

        return await _users.Users
            .AsNoTracking()
            .Where(u => memberIds.Contains(u.Id))
            .OrderBy(u => u.DisplayName)
            .Select(u => new FriendResponse(u.Id, u.DisplayName))
            .ToListAsync(ct);
    }

    public async Task<List<FriendRequestResponse>> GetPendingIncomingAsync(Guid userId, CancellationToken ct)
    {
        var requests = await _db.FriendRequests
            .AsNoTracking()
            .Where(r => r.ToUserId == userId && r.Status == RequestStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        var result = new List<FriendRequestResponse>();
        foreach (var r in requests)
        {
            var from = await _users.FindByIdAsync(r.FromUserId.ToString());
            var to = await _users.FindByIdAsync(r.ToUserId.ToString());
            result.Add(new FriendRequestResponse(
                r.Id, r.FromUserId, from?.DisplayName ?? "",
                r.ToUserId, to?.DisplayName ?? "",
                r.HouseholdWorkspaceId, r.Status, r.CreatedAt));
        }
        return result;
    }
}
