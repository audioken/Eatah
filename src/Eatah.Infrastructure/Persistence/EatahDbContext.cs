using Eatah.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Eatah.Infrastructure.Persistence;

public class EatahDbContext : DbContext
{
    public EatahDbContext(DbContextOptions<EatahDbContext> options) : base(options)
    {
    }

    public DbSet<Meal> Meals => Set<Meal>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<WeeklyPlan> WeeklyPlans => Set<WeeklyPlan>();
    public DbSet<DayPlan> DayPlans => Set<DayPlan>();
    public DbSet<DietProfile> DietProfiles => Set<DietProfile>();
    public DbSet<DietRule> DietRules => Set<DietRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EatahDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
