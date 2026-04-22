namespace Eatah.Client.Services;

/// <summary>
/// Tracks which ingredients have been checked for each meal so that
/// both the ingredient checklist modal and the day card badge stay in sync.
/// </summary>
public class IngredientCheckState
{
    private readonly Dictionary<Guid, IReadOnlyList<string>> _ingredients = new();
    private readonly Dictionary<Guid, HashSet<string>> _checked = new();

    public event Action? OnChange;

    public bool HasIngredients(Guid mealId) => _ingredients.ContainsKey(mealId);

    public IReadOnlyList<string> GetIngredients(Guid mealId) =>
        _ingredients.TryGetValue(mealId, out var list) ? list : Array.Empty<string>();

    public HashSet<string> GetChecked(Guid mealId)
    {
        if (!_checked.TryGetValue(mealId, out var set))
        {
            set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _checked[mealId] = set;
        }
        return set;
    }

    public void SetIngredients(Guid mealId, IEnumerable<string> ingredients)
    {
        _ingredients[mealId] = ingredients.ToList();
        _ = GetChecked(mealId); // ensure the checked set exists
        OnChange?.Invoke();
    }

    public void Toggle(Guid mealId, string name)
    {
        var set = GetChecked(mealId);
        if (!set.Add(name)) set.Remove(name);
        OnChange?.Invoke();
    }

    /// <summary>
    /// Returns true when all known ingredients for the meal are checked.
    /// Returns false when at least one is missing (or when ingredients are not yet known).
    /// </summary>
    public bool AreAllChecked(Guid mealId)
    {
        if (!_ingredients.TryGetValue(mealId, out var ings) || ings.Count == 0)
            return false;
        var set = GetChecked(mealId);
        return ings.All(set.Contains);
    }
}
