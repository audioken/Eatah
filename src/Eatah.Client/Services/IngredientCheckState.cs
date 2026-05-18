namespace Eatah.Client.Services;

/// <summary>
/// Tracks which ingredients have been checked for each meal so that
/// both the ingredient checklist modal and the day card badge stay in sync.
/// Pantry items are pre-checked automatically when ingredients are registered.
/// </summary>
public class IngredientCheckState
{
    private readonly Dictionary<Guid, IReadOnlyList<string>> _ingredients = new();
    private readonly Dictionary<Guid, HashSet<string>> _checked = new();
    private HashSet<string> _pantryNames = new(StringComparer.OrdinalIgnoreCase);
    // Meal IDs that are currently active in the viewed weekly plan.
    private HashSet<Guid> _activeMealIds = new();

    public event Action? OnChange;

    public bool HasIngredients(Guid mealId) => _ingredients.ContainsKey(mealId);

    public bool IsInPantry(string name) => _pantryNames.Contains(name);

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
        var list = ingredients.ToList();
        _ingredients[mealId] = list;
        var set = GetChecked(mealId);
        // Pre-check any ingredient that is already in the pantry.
        foreach (var name in list.Where(n => _pantryNames.Contains(n)))
            set.Add(name);
        OnChange?.Invoke();
    }

    /// <summary>
    /// Updates the set of pantry ingredient names. Should be called once at page load.
    /// Clears and rebuilds the checked state for all loaded meals so that additions
    /// and removals from the pantry are both reflected correctly.
    /// </summary>
    public void SetPantryItems(IEnumerable<string> pantryNames)
    {
        _pantryNames = new HashSet<string>(pantryNames, StringComparer.OrdinalIgnoreCase);
        // Rebuild checked state for every loaded meal from pantry only.
        // This discards stale checks (e.g. items removed from pantry since last load).
        foreach (var (mealId, ings) in _ingredients)
        {
            var set = GetChecked(mealId);
            set.Clear();
            foreach (var name in ings.Where(n => _pantryNames.Contains(n)))
                set.Add(name);
        }
        OnChange?.Invoke();
    }

    public void Toggle(Guid mealId, string name)
    {
        var set = GetChecked(mealId);
        if (!set.Add(name)) set.Remove(name);
        OnChange?.Invoke();
    }

    /// <summary>
    /// Marks a single ingredient name as pantry-present without rebuilding the full set.
    /// Call this after a successful AddToPantry API call for real-time UI sync.
    /// </summary>
    public void AddPantryName(string name)
    {
        _pantryNames.Add(name);
        // Pre-check this ingredient in every loaded meal that contains it.
        foreach (var (mealId, ings) in _ingredients)
        {
            if (ings.Contains(name, StringComparer.OrdinalIgnoreCase))
                GetChecked(mealId).Add(name);
        }
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

    /// <summary>
    /// Updates which meal IDs are active in the currently viewed weekly plan.
    /// Call this whenever the plan is loaded or mutated.
    /// </summary>
    public void SetActiveMeals(IEnumerable<Guid> mealIds)
    {
        _activeMealIds = new HashSet<Guid>(mealIds);
        OnChange?.Invoke();
    }

    /// <summary>
    /// Returns the number of ingredients for the given meal that are not in the pantry
    /// (i.e. still need to be purchased). Returns 0 when ingredients are not yet loaded.
    /// </summary>
    public int GetMissingCount(Guid mealId)
    {
        if (!_ingredients.TryGetValue(mealId, out var ings)) return 0;
        return ings.Count(n => !_pantryNames.Contains(n));
    }

    /// <summary>
    /// Returns how many distinct active meals in the current weekly plan contain
    /// the given ingredient name. Returns 0 if not yet known (ingredients not loaded).
    /// </summary>
    public int GetSharedMealCount(string ingredientName) =>
        _ingredients
            .Where(kv => _activeMealIds.Contains(kv.Key))
            .Count(kv => kv.Value.Any(n => string.Equals(n, ingredientName, StringComparison.OrdinalIgnoreCase)));
}
