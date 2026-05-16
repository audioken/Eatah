using Eatah.Domain.Entities;

namespace Eatah.Api.Features.Workspaces;

public record WorkspaceResponse(
    Guid Id,
    string Name,
    WorkspaceType Type,
    int MemberCount,
    bool IsOwner);

public record CreateHouseholdRequest(string Name);
public record RenameWorkspaceRequest(string Name);
