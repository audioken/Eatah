namespace Eatah.Domain.Entities;

public class DietProfile
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<DietRule> Rules { get; set; } = [];
}
