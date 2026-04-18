using Eatah.Api.Features.DietRules;
using Eatah.Domain.Entities;

namespace Eatah.Api.Features.WeeklyPlan;

public interface IRandomMealGenerator
{
    IReadOnlyList<Meal?> Generate(
        IReadOnlyList<Meal> availableMeals,
        IReadOnlyList<DayOfWeek> days,
        DietProfile? profile,
        double strictness);
}

public class RandomMealGenerator : IRandomMealGenerator
{
    private const int MaxIterations = 100;
    private readonly IDietRuleEvaluator _evaluator;

    public RandomMealGenerator(IDietRuleEvaluator evaluator)
    {
        _evaluator = evaluator;
    }

    public IReadOnlyList<Meal?> Generate(
        IReadOnlyList<Meal> availableMeals,
        IReadOnlyList<DayOfWeek> days,
        DietProfile? profile,
        double strictness)
    {
        ArgumentNullException.ThrowIfNull(availableMeals);
        ArgumentNullException.ThrowIfNull(days);

        var clampedStrictness = Math.Clamp(strictness, 0.0, 1.0);

        if (availableMeals.Count == 0 || days.Count == 0)
        {
            return new Meal?[days.Count];
        }

        // Without a profile or zero strictness -> single random assignment.
        if (profile is null || clampedStrictness <= 0.0)
        {
            return PickWithoutRepetition(availableMeals, days.Count, Random.Shared);
        }

        // Number of iterations grows with strictness (min 1, max MaxIterations).
        var iterations = Math.Max(1, (int)Math.Round(clampedStrictness * MaxIterations));

        IReadOnlyList<Meal?>? best = null;
        var bestScore = -1.0;

        for (var i = 0; i < iterations; i++)
        {
            var candidate = PickWithoutRepetition(availableMeals, days.Count, Random.Shared);
            var score = ScoreCandidate(candidate, days, profile);

            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
                if (score >= 1.0)
                {
                    break;
                }
            }
        }

        return best ?? PickWithoutRepetition(availableMeals, days.Count, Random.Shared);
    }

    private double ScoreCandidate(
        IReadOnlyList<Meal?> candidate,
        IReadOnlyList<DayOfWeek> days,
        DietProfile profile)
    {
        var virtualDays = new List<DayPlan>(candidate.Count);
        for (var i = 0; i < candidate.Count; i++)
        {
            virtualDays.Add(new DayPlan
            {
                Id = Guid.NewGuid(),
                DayOfWeek = days[i],
                MealId = candidate[i]?.Id,
                Meal = candidate[i]
            });
        }

        var virtualPlan = new Eatah.Domain.Entities.WeeklyPlan
        {
            Id = Guid.NewGuid(),
            Days = virtualDays
        };

        return _evaluator.Evaluate(virtualPlan, profile).OverallScore;
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
                // Refill if fewer meals than slots.
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
