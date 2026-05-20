namespace Eatah.Domain.Entities;

/// <summary>
/// Master ingredient catalog. System ingredients (WorkspaceId == null) are seeded
/// once and visible to all workspaces. Workspace-scoped ingredients allow each
/// household/personal space to add custom entries.
/// </summary>
public class IngredientMaster
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    /// <summary>Null = system ingredient visible to all workspaces.</summary>
    public Guid? WorkspaceId { get; set; }
}

/// <summary>An ingredient already at home (workspace pantry).</summary>
public class PantryItem
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid IngredientId { get; set; }
    public IngredientMaster? Ingredient { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Per-cooking-session answer to "does this pantry item cover this DayPlan?".
/// Keyed by DayPlanId rather than MealId so the same meal scheduled on multiple
/// days (or across weeks) gets independent coverage answers. Absence of a row
/// for a (PantryItem, DayPlan) pair means the question is still pending.
/// </summary>
public class PantryItemMealCoverage
{
    public Guid Id { get; set; }
    public Guid PantryItemId { get; set; }
    public PantryItem? PantryItem { get; set; }
    public Guid DayPlanId { get; set; }
    public bool Covers { get; set; }
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>An item to buy (workspace shopping list).</summary>
public class ShoppingItem
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid IngredientId { get; set; }
    public IngredientMaster? Ingredient { get; set; }
    public bool IsChecked { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Optional source annotation, e.g. "Tacos v17".</summary>
    public string? Notes { get; set; }
}
