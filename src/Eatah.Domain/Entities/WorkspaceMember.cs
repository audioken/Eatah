namespace Eatah.Domain.Entities;

public class WorkspaceMember
{
    public Guid WorkspaceId { get; set; }
    public Workspace Workspace { get; set; } = null!;
    public Guid UserId { get; set; }
    public MemberRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
}
