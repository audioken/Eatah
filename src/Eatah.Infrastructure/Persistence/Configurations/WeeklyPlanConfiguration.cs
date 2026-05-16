using Eatah.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eatah.Infrastructure.Persistence.Configurations;

public class WeeklyPlanConfiguration : IEntityTypeConfiguration<WeeklyPlan>
{
    public void Configure(EntityTypeBuilder<WeeklyPlan> builder)
    {
        builder.ToTable("weekly_plans");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Id)
            .HasColumnName("id");

        builder.Property(w => w.Year)
            .HasColumnName("year")
            .IsRequired();

        builder.Property(w => w.WeekNumber)
            .HasColumnName("week_number")
            .IsRequired();

        builder.Property(w => w.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(w => w.WorkspaceId)
            .HasColumnName("workspace_id")
            .IsRequired();

        builder.HasMany(w => w.Days)
            .WithOne()
            .HasForeignKey("weekly_plan_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(w => new { w.WorkspaceId, w.Year, w.WeekNumber }).IsUnique();
    }
}

public class DayPlanConfiguration : IEntityTypeConfiguration<DayPlan>
{
    public void Configure(EntityTypeBuilder<DayPlan> builder)
    {
        builder.ToTable("day_plans");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnName("id");

        builder.Property(d => d.DayOfWeek)
            .HasColumnName("day_of_week")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.MealId)
            .HasColumnName("meal_id");

        builder.Property<Guid>("weekly_plan_id");

        builder.HasOne(d => d.Meal)
            .WithMany()
            .HasForeignKey(d => d.MealId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
