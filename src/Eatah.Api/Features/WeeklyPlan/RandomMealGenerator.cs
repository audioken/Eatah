using Eatah.Api.Features.DietRules;
using Eatah.Domain.Entities;

namespace Eatah.Api.Features.WeeklyPlan;

public interface IRandomMealGenerator
{
    IReadOnlyList<Meal?> Generate(
        IReadOnlyList<Meal> availableMeals,
        IReadOnlyList<DayOfWeek> days,
        DietProfile? profile);

    Meal? GenerateForDay(
        IReadOnlyList<Meal> availableMeals,
        Eatah.Domain.Entities.WeeklyPlan currentPlan,
        DayOfWeek targetDay,
        DietProfile? profile);
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
        DietProfile? profile)
    {
        ArgumentNullException.ThrowIfNull(availableMeals);
        ArgumentNullException.ThrowIfNull(days);

        if (availableMeals.Count == 0 || days.Count == 0)
        {
            return new Meal?[days.Count];
        }

        if (profile is null)
        {
            return PickWithoutRepetition(availableMeals, days.Count, Random.Shared);
        }

        var iterations = MaxIterations;

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

    public Meal? GenerateForDay(
        IReadOnlyList<Meal> availableMeals,
        Eatah.Domain.Entities.WeeklyPlan currentPlan,
        DayOfWeek targetDay,
        DietProfile? profile)
    {
        ArgumentNullException.ThrowIfNull(availableMeals);
        ArgumentNullException.ThrowIfNull(currentPlan);

        if (availableMeals.Count == 0)
        {
            return null;
        }

        // Avoid picking meals already assigned to other days when possible.
        var assignedMealIds = currentPlan.Days
            .Where(d => d.DayOfWeek != targetDay && d.MealId.HasValue)
            .Select(d => d.MealId!.Value)
            .ToHashSet();

        var candidatePool = availableMeals.Where(m => !assignedMealIds.Contains(m.Id)).ToList();
        if (candidatePool.Count == 0)
        {
            candidatePool = availableMeals.ToList();
        }

        if (profile is null)
        {
            return candidatePool[Random.Shared.Next(candidatePool.Count)];
        }

        var iterations = MaxIterations;
        Meal? best = null;
        var bestScore = -1.0;

        for (var i = 0; i < iterations; i++)
        {
            var candidate = candidatePool[Random.Shared.Next(candidatePool.Count)];
            var score = ScoreSingleDayCandidate(candidate, currentPlan, targetDay, profile);
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

        return best ?? candidatePool[Random.Shared.Next(candidatePool.Count)];
    }

    private double ScoreSingleDayCandidate(
        Meal candidate,
        Eatah.Domain.Entities.WeeklyPlan currentPlan,
        DayOfWeek targetDay,
        DietProfile profile)
    {
        var virtualDays = currentPlan.Days.Select(d => new DayPlan
        {
            Id = Guid.NewGuid(),
            DayOfWeek = d.DayOfWeek,
            MealId = d.DayOfWeek == targetDay ? candidate.Id : d.MealId,
            Meal = d.DayOfWeek == targetDay ? candidate : d.Meal
        }).ToList();

        var virtualPlan = new Eatah.Domain.Entities.WeeklyPlan
        {
            Id = Guid.NewGuid(),
            Days = virtualDays
        };

        return _evaluator.Evaluate(virtualPlan, profile).OverallScore;
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
