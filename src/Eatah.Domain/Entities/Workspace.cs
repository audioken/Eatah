namespace Eatah.Domain.Entities;

public class Workspace
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public WorkspaceType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<WorkspaceMember> Members { get; set; } = [];
}
