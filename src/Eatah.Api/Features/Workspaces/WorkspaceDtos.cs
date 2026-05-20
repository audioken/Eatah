namespace Eatah.Api.Features.Workspaces;

public record WorkspaceResponse(
    Guid Id,
    string Name,
    int MemberCount,
    bool IsOwner);

public record CreateHouseholdRequest(string Name);
public record RenameWorkspaceRequest(string Name);
public record WorkspaceMemberResponse(Guid UserId, string DisplayName);
