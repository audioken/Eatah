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
    // Pantry-name → master IngredientId, populated alongside pantry items.
    private Dictionary<string, Guid> _pantryIngredientIds = new(StringComparer.OrdinalIgnoreCase);
    // Coverage answers per pantry ingredient: Covered = pantry stock covers the meal,
    // Declined = user said it doesn't. Absence in either = pending question.
    private readonly Dictionary<string, HashSet<Guid>> _coveredMeals = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<Guid>> _declinedMeals = new(StringComparer.OrdinalIgnoreCase);
    // Meal IDs that are currently active in the viewed weekly plan.
    private HashSet<Guid> _activeMealIds = new();
    // Display names for active meals, keyed by MealId. Populated alongside SetActiveMeals.
    private Dictionary<Guid, string> _activeMealNames = new();

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
    /// Same as <see cref="SetActiveMeals(IEnumerable{Guid})"/> but also records display names so coverage
    /// popups outside the dashboard can render meal labels by MealId.
    /// </summary>
    public void SetActiveMeals(IEnumerable<(Guid Id, string Name)> meals)
    {
        var list = meals.ToList();
        _activeMealIds = list.Select(m => m.Id).ToHashSet();
        _activeMealNames = list.GroupBy(m => m.Id).ToDictionary(g => g.Key, g => g.First().Name);
        OnChange?.Invoke();
    }

    /// <summary>Returns active meals (id+name) in the viewed plan that include this ingredient.</summary>
    public IReadOnlyList<(Guid Id, string Name)> GetActiveMealsForIngredient(string ingredientName)
    {
        return _ingredients
            .Where(kv => _activeMealIds.Contains(kv.Key)
                && kv.Value.Any(n => string.Equals(n, ingredientName, StringComparison.OrdinalIgnoreCase)))
            .Select(kv => (Id: kv.Key, Name: _activeMealNames.TryGetValue(kv.Key, out var n) ? n : string.Empty))
            .ToList();
    }

    /// <summary>
    /// Active meals containing this ingredient where pantry stock has NOT been marked as covering
    /// the meal yet (either explicitly declined, or unanswered). Use this to drive the coverage
    /// popup so previously-covered meals aren't re-asked.
    /// </summary>
    public IReadOnlyList<(Guid Id, string Name)> GetUncoveredActiveMealsForIngredient(string ingredientName)
    {
        var coveredSet = _coveredMeals.TryGetValue(ingredientName, out var c) ? c : new HashSet<Guid>();
        return _ingredients
            .Where(kv => _activeMealIds.Contains(kv.Key)
                && !coveredSet.Contains(kv.Key)
                && kv.Value.Any(n => string.Equals(n, ingredientName, StringComparison.OrdinalIgnoreCase)))
            .Select(kv => (Id: kv.Key, Name: _activeMealNames.TryGetValue(kv.Key, out var n) ? n : string.Empty))
            .ToList();
    }

    /// <summary>Number of active meals still needing this ingredient (i.e. not yet marked covered).</summary>
    public int GetUncoveredMealCount(string ingredientName) =>
        GetUncoveredActiveMealsForIngredient(ingredientName).Count;

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

    /// <summary>
    /// Replaces the pantry-name → ingredient-id lookup. Used so coverage operations can
    /// resolve the master id without a round-trip to the catalog API.
    /// </summary>
    public void SetPantryIngredientIds(IEnumerable<(string Name, Guid IngredientId)> items)
    {
        _pantryIngredientIds = items
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().IngredientId, StringComparer.OrdinalIgnoreCase);
    }

    public bool TryGetPantryIngredientId(string name, out Guid ingredientId) =>
        _pantryIngredientIds.TryGetValue(name, out ingredientId);

    /// <summary>
    /// Replaces all coverage state from a freshly fetched server map.
    /// Items keyed by pantry-ingredient name (lookup via SetPantryIngredientIds).
    /// </summary>
    public void SetCoverage(IEnumerable<(Guid IngredientId, Guid MealId, bool Covers)> rows)
    {
        _coveredMeals.Clear();
        _declinedMeals.Clear();
        // Invert ingredientId → name using known pantry mapping.
        var idToName = _pantryIngredientIds.ToDictionary(kv => kv.Value, kv => kv.Key);
        foreach (var row in rows)
        {
            if (!idToName.TryGetValue(row.IngredientId, out var name)) continue;
            var bucket = row.Covers ? _coveredMeals : _declinedMeals;
            if (!bucket.TryGetValue(name, out var set))
            {
                set = new HashSet<Guid>();
                bucket[name] = set;
            }
            set.Add(row.MealId);
        }
        OnChange?.Invoke();
    }

    public void MarkCoverage(string ingredientName, Guid mealId, bool covers)
    {
        // Remove from the opposite bucket first to keep state consistent.
        if (covers && _declinedMeals.TryGetValue(ingredientName, out var d)) d.Remove(mealId);
        else if (!covers && _coveredMeals.TryGetValue(ingredientName, out var c)) c.Remove(mealId);

        var bucket = covers ? _coveredMeals : _declinedMeals;
        if (!bucket.TryGetValue(ingredientName, out var set))
        {
            set = new HashSet<Guid>();
            bucket[ingredientName] = set;
        }
        set.Add(mealId);
        OnChange?.Invoke();
    }

    /// <summary>
    /// Coverage stats for an ingredient across the currently-active weekly plan.
    /// Covered = pantry stock confirmed to cover that meal. Total = meals in the active
    /// week containing this ingredient.
    /// </summary>
    public (int Covered, int Total) GetCoverageFraction(string ingredientName)
    {
        var needingMealIds = _ingredients
            .Where(kv => _activeMealIds.Contains(kv.Key)
                && kv.Value.Any(n => string.Equals(n, ingredientName, StringComparison.OrdinalIgnoreCase)))
            .Select(kv => kv.Key)
            .ToList();
        var covered = _coveredMeals.TryGetValue(ingredientName, out var set)
            ? needingMealIds.Count(set.Contains)
            : 0;
        return (covered, needingMealIds.Count);
    }

    /// <summary>True when at least one pantry ingredient that this meal needs has not been
    /// answered yet (neither covered nor declined for this meal).</summary>
    public bool HasPendingCoverageQuestion(Guid mealId)
    {
        if (!_ingredients.TryGetValue(mealId, out var ings)) return false;
        foreach (var name in ings)
        {
            if (!_pantryNames.Contains(name)) continue;
            var covered = _coveredMeals.TryGetValue(name, out var c) && c.Contains(mealId);
            var declined = _declinedMeals.TryGetValue(name, out var d) && d.Contains(mealId);
            if (!covered && !declined) return true;
        }
        return false;
    }

    /// <summary>Pantry ingredient names where coverage for this meal is still unanswered.</summary>
    public IEnumerable<string> GetPendingCoverageIngredients(Guid mealId)
    {
        if (!_ingredients.TryGetValue(mealId, out var ings)) return [];
        return ings.Where(name =>
        {
            if (!_pantryNames.Contains(name)) return false;
            var covered = _coveredMeals.TryGetValue(name, out var c) && c.Contains(mealId);
            var declined = _declinedMeals.TryGetValue(name, out var d) && d.Contains(mealId);
            return !covered && !declined;
        });
    }

    public bool IsCoveredForMeal(string ingredientName, Guid mealId) =>
        _coveredMeals.TryGetValue(ingredientName, out var set) && set.Contains(mealId);
}
