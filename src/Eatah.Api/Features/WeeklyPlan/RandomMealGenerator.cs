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

        // Avoid picking meals already assigned to other days when possible.
        var assignedMealIds = currentPlan.Days
            .Where(d => d.DayOfWeek != targetDay && d.MealId.HasValue)
            .Select(d => d.MealId!.Value)
            .ToHashSet();

        var candidatePool = availableMeals.Where(m => !assignedMealIds.Contains(m.Id)).ToList();
        if (candidatePool.Count == 0)
            candidatePool = availableMeals.ToList();

        if (profile is null)
            return candidatePool[Random.Shared.Next(candidatePool.Count)];

        // Count category usage in the current plan (excluding the target day).
        var currentCounts = currentPlan.Days
            .Where(d => d.DayOfWeek != targetDay && d.Meal is not null)
            .GroupBy(d => d.Meal!.Category)
            .ToDictionary(g => g.Key, g => g.Count());

        // Remove explicitly excluded categories (max == 0) from the candidate pool.
        var excludedCategories = profile.Rules
            .Where(r => r.MaxPerWeek == 0)
            .Select(r => r.Category)
            .ToHashSet();

        candidatePool = candidatePool.Where(m => !excludedCategories.Contains(m.Category)).ToList();
        if (candidatePool.Count == 0)
            return null;

        // Prefer categories that are still below their minimum.
        var neededCategories = profile.Rules
            .Where(r => r.MaxPerWeek > 0 && currentCounts.GetValueOrDefault(r.Category, 0) < r.MinPerWeek)
            .OrderByDescending(r => r.MinPerWeek - currentCounts.GetValueOrDefault(r.Category, 0))
            .Select(r => r.Category)
            .ToList();

        foreach (var category in neededCategories)
        {
            var matching = candidatePool.Where(m => m.Category == category).ToList();
            if (matching.Count > 0)
                return matching[Random.Shared.Next(matching.Count)];
        }

        // Otherwise pick any meal that won't exceed its category maximum.
        // If the profile has no rule for a category, that category is not allowed
        // either — the profile must explicitly opt categories in.
        var allowed = candidatePool.Where(m =>
        {
            var rule = profile.Rules.FirstOrDefault(r => r.Category == m.Category);
            if (rule is null) return false;
            return currentCounts.GetValueOrDefault(m.Category, 0) < rule.MaxPerWeek;
        }).ToList();

        if (allowed.Count > 0)
            return allowed[Random.Shared.Next(allowed.Count)];

        // Strict mode: no candidate fits the profile. Leave the slot empty rather
        // than violating the rules.
        return null;
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
