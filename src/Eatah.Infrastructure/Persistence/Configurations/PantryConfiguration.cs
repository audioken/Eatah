using Eatah.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eatah.Infrastructure.Persistence.Configurations;

public class IngredientMasterConfiguration : IEntityTypeConfiguration<IngredientMaster>
{
    public void Configure(EntityTypeBuilder<IngredientMaster> b)
    {
        b.ToTable("ingredient_master");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(120);
        b.Property(x => x.Category).HasColumnName("category").HasMaxLength(60);
        b.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        b.HasIndex(x => new { x.WorkspaceId, x.Name }).IsUnique();
    }
}

public class PantryItemConfiguration : IEntityTypeConfiguration<PantryItem>
{
    public void Configure(EntityTypeBuilder<PantryItem> b)
    {
        b.ToTable("pantry_items");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        b.Property(x => x.IngredientId).HasColumnName("ingredient_id");
        b.Property(x => x.AddedAt).HasColumnName("added_at").HasDefaultValueSql("NOW()");
        b.HasOne(x => x.Ingredient).WithMany().HasForeignKey(x => x.IngredientId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.WorkspaceId, x.IngredientId }).IsUnique();
        // Optimistic concurrency: detect concurrent updates from other workspace members.
        b.UseXminAsConcurrencyToken();
    }
}

public class PantryItemMealCoverageConfiguration : IEntityTypeConfiguration<PantryItemMealCoverage>
{
    public void Configure(EntityTypeBuilder<PantryItemMealCoverage> b)
    {
        b.ToTable("pantry_item_meal_coverages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.PantryItemId).HasColumnName("pantry_item_id");
        b.Property(x => x.DayPlanId).HasColumnName("day_plan_id");
        b.Property(x => x.Covers).HasColumnName("covers");
        b.Property(x => x.AnsweredAt).HasColumnName("answered_at").HasDefaultValueSql("NOW()");
        b.HasOne(x => x.PantryItem).WithMany().HasForeignKey(x => x.PantryItemId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<DayPlan>().WithMany().HasForeignKey(x => x.DayPlanId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.PantryItemId, x.DayPlanId }).IsUnique();
        b.UseXminAsConcurrencyToken();
    }
}

public class ShoppingItemConfiguration : IEntityTypeConfiguration<ShoppingItem>
{
    public void Configure(EntityTypeBuilder<ShoppingItem> b)
    {
        b.ToTable("shopping_items");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        b.Property(x => x.IngredientId).HasColumnName("ingredient_id");
        b.Property(x => x.IsChecked).HasColumnName("is_checked");
        b.Property(x => x.AddedAt).HasColumnName("added_at").HasDefaultValueSql("NOW()");
        b.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(2000);
        b.HasOne(x => x.Ingredient).WithMany().HasForeignKey(x => x.IngredientId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.WorkspaceId, x.IngredientId }).IsUnique();
        b.UseXminAsConcurrencyToken();
    }
}
