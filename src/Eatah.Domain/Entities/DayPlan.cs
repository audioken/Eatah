namespace Eatah.Domain.Entities;

public class DayPlan
{
    public Guid Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public Guid? MealId { get; set; }
    public Meal? Meal { get; set; }
    public MealConfirmationStatus? ConfirmationStatus { get; set; }
    public DateTime? ConfirmedAt { get; set; }
}
