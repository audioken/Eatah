namespace Eatah.Domain.Entities;

public class DietRule
{
    public Guid Id { get; set; }
    public MealCategory Category { get; set; }
    public int MinPerWeek { get; set; }
    public int MaxPerWeek { get; set; }
    public string Description { get; set; } = string.Empty;
}
