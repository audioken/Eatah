namespace Eatah.Domain.Entities;

public class DietProfile
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<DietRule> Rules { get; set; } = [];
    /// <summary>Null = system profile (e.g. Livsmedelsverket) visible to all workspaces.</summary>
    public Guid? WorkspaceId { get; set; }
}
