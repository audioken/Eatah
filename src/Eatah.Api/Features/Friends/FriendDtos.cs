using Eatah.Domain.Entities;

namespace Eatah.Api.Features.Friends;

public record UserSearchResult(Guid Id, string DisplayName);
public record SendFriendRequestRequest(Guid ToUserId);
public record RespondToFriendRequestRequest(bool Accept);
public record FriendRequestResponse(
    Guid Id, Guid FromUserId, string FromDisplayName, Guid ToUserId, string ToDisplayName,
    Guid HouseholdWorkspaceId, RequestStatus Status, DateTime CreatedAt);
public record FriendResponse(Guid Id, string DisplayName);
