using Eatah.Domain.Entities;

namespace Eatah.Api.Features.DietRules;

public class DietEvaluation
{
    public double OverallScore { get; set; }
    public List<RuleResult> RuleResults { get; set; } = [];
}

public class RuleResult
{
    public string RuleName { get; set; } = string.Empty;
    public MealCategory Category { get; set; }
    public bool IsMet { get; set; }
    public int Actual { get; set; }
    public int Min { get; set; }
    public int Max { get; set; }
    public double Score { get; set; }
    public string Message { get; set; } = string.Empty;
}

public interface IDietRuleEvaluator
{
    DietEvaluation Evaluate(Eatah.Domain.Entities.WeeklyPlan plan, DietProfile profile);
}

public class DietRuleEvaluator : IDietRuleEvaluator
{
    public DietEvaluation Evaluate(Eatah.Domain.Entities.WeeklyPlan plan, DietProfile profile)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(profile);

        var countsByCategory = plan.Days
            .Where(d => d.Meal is not null)
            .GroupBy(d => d.Meal!.Category)
            .ToDictionary(g => g.Key, g => g.Count());

        var results = new List<RuleResult>();

        foreach (var rule in profile.Rules)
        {
            var actual = countsByCategory.TryGetValue(rule.Category, out var c) ? c : 0;
            var (score, isMet, message) = ScoreRule(rule, actual);

            results.Add(new RuleResult
            {
                RuleName = rule.Description,
                Category = rule.Category,
                Actual = actual,
                Min = rule.MinPerWeek,
                Max = rule.MaxPerWeek,
                Score = score,
                IsMet = isMet,
                Message = message
            });
        }

        var overall = results.Count == 0 ? 1.0 : results.Average(r => r.Score);

        return new DietEvaluation
        {
            OverallScore = overall,
            RuleResults = results
        };
    }

    private static (double Score, bool IsMet, string Message) ScoreRule(DietRule rule, int actual)
    {
        if (actual >= rule.MinPerWeek && actual <= rule.MaxPerWeek)
        {
            return (1.0, true, $"{rule.Category}: {actual} occurrences – within recommendation.");
        }

        if (actual < rule.MinPerWeek)
        {
            var score = rule.MinPerWeek == 0 ? 1.0 : (double)actual / rule.MinPerWeek;
            return (Math.Clamp(score, 0.0, 1.0), false,
                $"{rule.Category}: {actual} occurrences – below minimum ({rule.MinPerWeek}).");
        }

        // actual > max
        var over = actual - rule.MaxPerWeek;
        var denominator = Math.Max(1, rule.MaxPerWeek == 0 ? 7 : rule.MaxPerWeek);
        var penalty = (double)over / denominator;
        var overScore = Math.Clamp(1.0 - penalty, 0.0, 1.0);
        return (overScore, false,
            $"{rule.Category}: {actual} occurrences – above maximum ({rule.MaxPerWeek}).");
    }
}
