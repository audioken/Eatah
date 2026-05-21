namespace Eatah.Client.Services;

/// <summary>
/// Tracks ingredient state for the meal-planner UI: which ingredients each meal has,
/// which ones are in pantry, and per-DayPlan coverage answers ("does my pantry stock
/// cover this particular cooking session?").
///
/// Two different identifiers are used on purpose:
///   - <b>MealId</b> for recipe data (a meal's ingredient list), which is shared across
///     every DayPlan that uses the meal.
///   - <b>DayPlanId</b> for coverage answers, so the same meal scheduled on multiple
///     days (or across weeks) is asked independently.
/// </summary>
public class IngredientCheckState
{
    private readonly Dictionary<Guid, IReadOnlyList<string>> _ingredients = new();
    private readonly Dictionary<Guid, HashSet<string>> _checked = new();
    private HashSet<string> _pantryNames = new(StringComparer.OrdinalIgnoreCase);
    // Pantry-name → master IngredientId, populated alongside pantry items.
    private Dictionary<string, Guid> _pantryIngredientIds = new(StringComparer.OrdinalIgnoreCase);
    // Per-session coverage answers, keyed by ingredient name → set of DayPlanIds.
    // Covered = pantry stock covers the session. Declined = user said it doesn't.
    // Absence from either = pending question.
    private readonly Dictionary<string, HashSet<Guid>> _coveredSessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<Guid>> _declinedSessions = new(StringComparer.OrdinalIgnoreCase);
    // Sessions currently visible in the planner. Keyed by DayPlanId so multiple DayPlans
    // pointing at the same meal stay distinct.
    private Dictionary<Guid, ActiveSession> _activeSessions = new();

    public record ActiveSession(Guid DayPlanId, Guid MealId, string MealName, int DayIndex, int? WeekNumber = null);

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
        foreach (var (mealId, ings) in _ingredients)
        {
            if (ings.Contains(name, StringComparer.OrdinalIgnoreCase))
                GetChecked(mealId).Add(name);
        }
        OnChange?.Invoke();
    }

    /// <summary>
    /// Removes a single ingredient name from the pantry set without rebuilding the full set.
    /// Call this after a successful RemoveFromPantry API call for real-time UI sync.
    /// Also clears all coverage answers for this ingredient since they are no longer valid.
    /// </summary>
    public void RemovePantryName(string name)
    {
        _pantryNames.Remove(name);
        foreach (var (mealId, ings) in _ingredients)
        {
            if (ings.Contains(name, StringComparer.OrdinalIgnoreCase))
                GetChecked(mealId).Remove(name);
        }
        _coveredSessions.Remove(name);
        _declinedSessions.Remove(name);
        _pantryIngredientIds.Remove(name);
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
    /// Updates which DayPlans are active in the currently viewed weekly plan(s).
    /// Coverage popups and badges use this to know which sessions are still "live".
    /// </summary>
    public void SetActiveSessions(IEnumerable<ActiveSession> sessions)
    {
        _activeSessions = sessions
            .GroupBy(s => s.DayPlanId)
            .ToDictionary(g => g.Key, g => g.First());
        OnChange?.Invoke();
    }

    /// <summary>Active sessions in the viewed plan whose meal includes this ingredient.</summary>
    public IReadOnlyList<ActiveSession> GetActiveSessionsForIngredient(string ingredientName)
    {
        return _activeSessions.Values
            .Where(s => _ingredients.TryGetValue(s.MealId, out var ings)
                && ings.Any(n => string.Equals(n, ingredientName, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    /// <summary>
    /// Active sessions containing this ingredient where pantry stock has NOT yet been
    /// marked as covering the session (either explicitly declined or unanswered).
    /// </summary>
    public IReadOnlyList<ActiveSession> GetUncoveredActiveSessionsForIngredient(string ingredientName)
    {
        var coveredSet = _coveredSessions.TryGetValue(ingredientName, out var c) ? c : new HashSet<Guid>();
        return GetActiveSessionsForIngredient(ingredientName)
            .Where(s => !coveredSet.Contains(s.DayPlanId))
            .ToList();
    }

    /// <summary>Number of active sessions still needing this ingredient (not yet marked covered).</summary>
    public int GetUncoveredSessionCount(string ingredientName) =>
        GetUncoveredActiveSessionsForIngredient(ingredientName).Count;

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
    /// Number of distinct active sessions in the planner whose meal contains this
    /// ingredient. Returns 0 if not yet known.
    /// </summary>
    public int GetSharedSessionCount(string ingredientName) =>
        GetActiveSessionsForIngredient(ingredientName).Count;

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
    public void SetCoverage(IEnumerable<(Guid IngredientId, Guid DayPlanId, bool Covers)> rows)
    {
        _coveredSessions.Clear();
        _declinedSessions.Clear();
        var idToName = _pantryIngredientIds.ToDictionary(kv => kv.Value, kv => kv.Key);
        foreach (var row in rows)
        {
            if (!idToName.TryGetValue(row.IngredientId, out var name)) continue;
            var bucket = row.Covers ? _coveredSessions : _declinedSessions;
            if (!bucket.TryGetValue(name, out var set))
            {
                set = new HashSet<Guid>();
                bucket[name] = set;
            }
            set.Add(row.DayPlanId);
        }
        OnChange?.Invoke();
    }

    public void MarkCoverage(string ingredientName, Guid dayPlanId, bool covers)
    {
        // Remove from the opposite bucket first to keep state consistent.
        if (covers && _declinedSessions.TryGetValue(ingredientName, out var d)) d.Remove(dayPlanId);
        else if (!covers && _coveredSessions.TryGetValue(ingredientName, out var c)) c.Remove(dayPlanId);

        var bucket = covers ? _coveredSessions : _declinedSessions;
        if (!bucket.TryGetValue(ingredientName, out var set))
        {
            set = new HashSet<Guid>();
            bucket[ingredientName] = set;
        }
        set.Add(dayPlanId);
        OnChange?.Invoke();
    }

    /// <summary>
    /// Coverage stats for an ingredient across the currently-active sessions.
    /// Covered = pantry stock confirmed to cover that session. Total = active sessions
    /// whose meal contains this ingredient.
    /// </summary>
    public (int Covered, int Total) GetCoverageFraction(string ingredientName)
    {
        var sessions = GetActiveSessionsForIngredient(ingredientName);
        var covered = _coveredSessions.TryGetValue(ingredientName, out var set)
            ? sessions.Count(s => set.Contains(s.DayPlanId))
            : 0;
        return (covered, sessions.Count);
    }

    /// <summary>
    /// True when at least one pantry ingredient that this session's meal needs has not
    /// yet been answered (neither covered nor declined) for THIS DayPlan.
    /// </summary>
    public bool HasPendingCoverageQuestion(Guid dayPlanId, Guid mealId)
    {
        if (!_ingredients.TryGetValue(mealId, out var ings)) return false;
        foreach (var name in ings)
        {
            if (!_pantryNames.Contains(name)) continue;
            var covered = _coveredSessions.TryGetValue(name, out var c) && c.Contains(dayPlanId);
            var declined = _declinedSessions.TryGetValue(name, out var dd) && dd.Contains(dayPlanId);
            if (!covered && !declined) return true;
        }
        return false;
    }

    /// <summary>Pantry ingredient names where coverage for this DayPlan is still unanswered.</summary>
    public IEnumerable<string> GetPendingCoverageIngredients(Guid dayPlanId, Guid mealId)
    {
        if (!_ingredients.TryGetValue(mealId, out var ings)) return [];
        return ings.Where(name =>
        {
            if (!_pantryNames.Contains(name)) return false;
            var covered = _coveredSessions.TryGetValue(name, out var c) && c.Contains(dayPlanId);
            var declined = _declinedSessions.TryGetValue(name, out var dd) && dd.Contains(dayPlanId);
            return !covered && !declined;
        });
    }

    public bool IsCoveredForSession(string ingredientName, Guid dayPlanId) =>
        _coveredSessions.TryGetValue(ingredientName, out var set) && set.Contains(dayPlanId);

    /// <summary>
    /// Clears all coverage answers (covered and declined) for the given DayPlan IDs.
    /// Call this whenever a meal is swapped or randomised so the new meal's pantry
    /// ingredients are treated as pending and the user is asked again.
    /// </summary>
    public void ClearCoverageForDayPlans(IEnumerable<Guid> dayPlanIds)
    {
        var ids = new HashSet<Guid>(dayPlanIds);
        foreach (var name in _coveredSessions.Keys.ToList())
            _coveredSessions[name].ExceptWith(ids);
        foreach (var name in _declinedSessions.Keys.ToList())
            _declinedSessions[name].ExceptWith(ids);
        OnChange?.Invoke();
    }
}
