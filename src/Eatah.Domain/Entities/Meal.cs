namespace Eatah.Domain.Entities;

public class Meal
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Ingredient> Ingredients { get; set; } = [];
    public MealCategory Category { get; set; }
    public int? CookingTimeMinutes { get; set; }
    public DateTime CreatedAt { get; set; }
}
