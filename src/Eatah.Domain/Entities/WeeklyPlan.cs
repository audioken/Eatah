namespace Eatah.Domain.Entities;

public class WeeklyPlan
{
    public Guid Id { get; set; }
    public int Year { get; set; }
    public int WeekNumber { get; set; }
    public List<DayPlan> Days { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}
