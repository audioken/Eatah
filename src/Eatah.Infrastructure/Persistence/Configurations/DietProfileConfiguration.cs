using Eatah.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eatah.Infrastructure.Persistence.Configurations;

public class DietProfileConfiguration : IEntityTypeConfiguration<DietProfile>
{
    public void Configure(EntityTypeBuilder<DietProfile> builder)
    {
        builder.ToTable("diet_profiles");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id");

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(200);

        builder.HasMany(p => p.Rules)
            .WithOne()
            .HasForeignKey("diet_profile_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.Name).IsUnique();
    }
}

public class DietRuleConfiguration : IEntityTypeConfiguration<DietRule>
{
    public void Configure(EntityTypeBuilder<DietRule> builder)
    {
        builder.ToTable("diet_rules");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id");

        builder.Property(r => r.Category)
            .HasColumnName("category")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.MinPerWeek)
            .HasColumnName("min_per_week")
            .IsRequired();

        builder.Property(r => r.MaxPerWeek)
            .HasColumnName("max_per_week")
            .IsRequired();

        builder.Property(r => r.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property<Guid>("diet_profile_id");
    }
}
