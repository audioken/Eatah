using Eatah.Domain.Entities;

namespace Eatah.Api.Features.WeeklyPlan;

public interface IRandomMealGenerator
{
    IReadOnlyList<Meal?> Generate(
        IReadOnlyList<Meal> availableMeals,
        IReadOnlyList<DayOfWeek> days,
        DietProfile? profile,
        IReadOnlyDictionary<MealCategory, int>? preAssignedCounts = null);

    Meal? GenerateForDay(
        IReadOnlyList<Meal> availableMeals,
        Eatah.Domain.Entities.WeeklyPlan currentPlan,
        DayOfWeek targetDay,
        DietProfile? profile);
}

public class RandomMealGenerator : IRandomMealGenerator
{
    public IReadOnlyList<Meal?> Generate(
        IReadOnlyList<Meal> availableMeals,
        IReadOnlyList<DayOfWeek> days,
        DietProfile? profile,
        IReadOnlyDictionary<MealCategory, int>? preAssignedCounts = null)
    {
        ArgumentNullException.ThrowIfNull(availableMeals);
        ArgumentNullException.ThrowIfNull(days);

        if (availableMeals.Count == 0 || days.Count == 0)
            return new Meal?[days.Count];

        if (profile is null)
            return PickWithoutRepetition(availableMeals, days.Count, Random.Shared);

        return GenerateWithConstraints(availableMeals, days.Count, profile.Rules, preAssignedCounts ?? new Dictionary<MealCategory, int>());
    }

    public Meal? GenerateForDay(
        IReadOnlyList<Meal> availableMeals,
        Eatah.Domain.Entities.WeeklyPlan currentPlan,
        DayOfWeek targetDay,
        DietProfile? profile)
    {
        ArgumentNullException.ThrowIfNull(availableMeals);
        ArgumentNullException.ThrowIfNull(currentPlan);

        if (availableMeals.Count == 0)
            return null;

        var assignedMealIds = currentPlan.Days
            .Where(d => d.DayOfWeek != targetDay && d.MealId.HasValue)
            .Select(d => d.MealId!.Value)
            .ToHashSet();

        if (profile is null || profile.Rules.Count == 0)
        {
            // No profile: prefer meals not already used this week, fall back to any meal.
            var unused = availableMeals.Where(m => !assignedMealIds.Contains(m.Id)).ToList();
            var pool = unused.Count > 0 ? unused : availableMeals.ToList();
            return pool[Random.Shared.Next(pool.Count)];
        }

        // Only count categories that the current profile knows about and allows.
        // This way, if the user switched profile mid-week, meals on past days whose
        // category isn't in the new profile don't eat into the new budget.
        var rulesByCategory = profile.Rules.ToDictionary(r => r.Category);
        var currentCounts = new Dictionary<MealCategory, int>();
        foreach (var day in currentPlan.Days)
        {
            if (day.DayOfWeek == targetDay || day.Meal is null) continue;
            if (!rulesByCategory.TryGetValue(day.Meal.Category, out var rule) || rule.MaxPerWeek <= 0) continue;
            currentCounts[day.Meal.Category] = currentCounts.GetValueOrDefault(day.Meal.Category, 0) + 1;
        }

        var allowedRules = profile.Rules
            .Where(r => r.MaxPerWeek > 0 && currentCounts.GetValueOrDefault(r.Category, 0) < r.MaxPerWeek)
            .ToList();
        if (allowedRules.Count == 0)
            return null;

        // Prefer categories still below their minimum; otherwise any allowed category.
        var neededRules = allowedRules
            .Where(r => currentCounts.GetValueOrDefault(r.Category, 0) < r.MinPerWeek)
            .OrderByDescending(r => r.MinPerWeek - currentCounts.GetValueOrDefault(r.Category, 0))
            .ToList();

        var preferredCategories = (neededRules.Count > 0 ? neededRules : allowedRules)
            .Select(r => r.Category)
            .ToHashSet();

        // Pick within the preferred categories first; if no meals exist there, widen
        // to any allowed category. Within the chosen pool we prefer meals not already
        // used this week, but always fall back to repetition before returning null —
        // as long as meals exist in an allowed category, we must return one.
        var meal = PickFromCategories(availableMeals, preferredCategories, assignedMealIds);
        if (meal is not null) return meal;

        var anyAllowedCategories = allowedRules.Select(r => r.Category).ToHashSet();
        return PickFromCategories(availableMeals, anyAllowedCategories, assignedMealIds);
    }

    private static Meal? PickFromCategories(
        IReadOnlyList<Meal> availableMeals,
        IReadOnlySet<MealCategory> categories,
        IReadOnlySet<Guid> assignedMealIds)
    {
        var candidates = availableMeals.Where(m => categories.Contains(m.Category)).ToList();
        if (candidates.Count == 0) return null;

        var unused = candidates.Where(m => !assignedMealIds.Contains(m.Id)).ToList();
        var pool = unused.Count > 0 ? unused : candidates;
        return pool[Random.Shared.Next(pool.Count)];
    }

    // Builds a week plan that strictly satisfies the profile rules.
    // Slots that cannot be filled (not enough meals of a required category) are left null.
    private static Meal?[] GenerateWithConstraints(
        IReadOnlyList<Meal> availableMeals,
        int slotCount,
        IReadOnlyList<DietRule> rules,
        IReadOnlyDictionary<MealCategory, int> preAssignedCounts)
    {
        var assigned = new List<Meal?>();
        var usedIds = new HashSet<Guid>();

        foreach (var rule in rules)
        {
            if (assigned.Count >= slotCount) break;
            if (rule.MaxPerWeek == 0) continue; // explicitly excluded

            var preCount = preAssignedCounts.GetValueOrDefault(rule.Category, 0);

            // How many more of this category the max still allows.
            var remainingMax = rule.MaxPerWeek - preCount;
            if (remainingMax <= 0) continue;

            var totalTarget = rule.MinPerWeek == rule.MaxPerWeek
                ? rule.MinPerWeek
                : Random.Shared.Next(rule.MinPerWeek, rule.MaxPerWeek + 1);

            // Subtract already-assigned (past days) from the total target.
            var additionalTarget = Math.Max(0, Math.Min(totalTarget - preCount, remainingMax));
            if (additionalTarget <= 0) continue;

            var categoryMeals = availableMeals
                .Where(m => m.Category == rule.Category && !usedIds.Contains(m.Id))
                .ToList();
            Shuffle(categoryMeals, Random.Shared);

            // Assign as many as available, up to the target and remaining slots.
            // If fewer meals exist than required, we leave those slots unfilled (null at the end).
            var toAssign = Math.Min(additionalTarget, Math.Min(categoryMeals.Count, slotCount - assigned.Count));
            for (var i = 0; i < toAssign; i++)
            {
                assigned.Add(categoryMeals[i]);
                usedIds.Add(categoryMeals[i].Id);
            }
        }

        // Randomize the order of the assigned meals across the days.
        Shuffle(assigned, Random.Shared);

        // Pad with nulls so unfilled days are left empty.
        while (assigned.Count < slotCount)
            assigned.Add(null);

        return assigned.ToArray();
    }

    private static Meal?[] PickWithoutRepetition(IReadOnlyList<Meal> meals, int count, Random random)
    {
        var result = new Meal?[count];
        var pool = meals.ToList();
        Shuffle(pool, random);

        for (var i = 0; i < count; i++)
        {
            if (pool.Count == 0)
            {
                pool.AddRange(meals);
                Shuffle(pool, random);
            }

            result[i] = pool[0];
            pool.RemoveAt(0);
        }

        return result;
    }

    private static void Shuffle<T>(IList<T> list, Random random)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
