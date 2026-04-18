using Eatah.Api.Features.DietRules;
using Eatah.Domain.Entities;

namespace Eatah.Api.Tests.Unit;

public class DietRuleEvaluatorTests
{
    private readonly DietRuleEvaluator _sut = new();

    [Fact]
    public void Evaluate_ShouldReturnFullScore_WhenAllRulesAreMet()
    {
        var profile = BuildProfile();
        var plan = BuildPlan(
            (DayOfWeek.Monday, MealCategory.Fish),
            (DayOfWeek.Tuesday, MealCategory.Fish),
            (DayOfWeek.Wednesday, MealCategory.Meat),
            (DayOfWeek.Thursday, MealCategory.Meat),
            (DayOfWeek.Friday, MealCategory.Vegetarian),
            (DayOfWeek.Saturday, MealCategory.Vegetarian),
            (DayOfWeek.Sunday, MealCategory.Vegan));

        var result = _sut.Evaluate(plan, profile);

        result.OverallScore.Should().Be(1.0);
        result.RuleResults.Should().OnlyContain(r => r.IsMet);
    }

    [Fact]
    public void Evaluate_ShouldFlagRuleAsUnmet_WhenBelowMinimum()
    {
        var profile = BuildProfile();
        var plan = BuildPlan(
            (DayOfWeek.Monday, MealCategory.Fish),
            (DayOfWeek.Tuesday, MealCategory.Meat),
            (DayOfWeek.Wednesday, MealCategory.Meat),
            (DayOfWeek.Thursday, MealCategory.Vegetarian),
            (DayOfWeek.Friday, MealCategory.Vegetarian),
            (DayOfWeek.Saturday, MealCategory.Vegetarian),
            (DayOfWeek.Sunday, MealCategory.Meat));

        var result = _sut.Evaluate(plan, profile);

        var fishResult = result.RuleResults.Single(r => r.Category == MealCategory.Fish);
        fishResult.IsMet.Should().BeFalse();
        fishResult.Actual.Should().Be(1);
        fishResult.Score.Should().BeLessThan(1.0);
    }

    [Fact]
    public void Evaluate_ShouldFlagRuleAsUnmet_WhenAboveMaximum()
    {
        var profile = new DietProfile
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Rules =
            [
                new DietRule { Category = MealCategory.Meat, MinPerWeek = 0, MaxPerWeek = 2, Description = "Max 2 kött" }
            ]
        };
        var plan = BuildPlan(
            (DayOfWeek.Monday, MealCategory.Meat),
            (DayOfWeek.Tuesday, MealCategory.Meat),
            (DayOfWeek.Wednesday, MealCategory.Meat),
            (DayOfWeek.Thursday, MealCategory.Meat),
            (DayOfWeek.Friday, MealCategory.Meat));

        var result = _sut.Evaluate(plan, profile);

        var meatResult = result.RuleResults.Single();
        meatResult.IsMet.Should().BeFalse();
        meatResult.Actual.Should().Be(5);
        meatResult.Score.Should().BeLessThan(1.0);
    }

    [Fact]
    public void Evaluate_ShouldReturnOne_WhenProfileHasNoRules()
    {
        var profile = new DietProfile { Id = Guid.NewGuid(), Name = "Empty", Rules = [] };
        var plan = BuildPlan();

        var result = _sut.Evaluate(plan, profile);

        result.OverallScore.Should().Be(1.0);
        result.RuleResults.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_ShouldThrow_WhenPlanIsNull()
    {
        var profile = BuildProfile();
        Action act = () => _sut.Evaluate(null!, profile);
        act.Should().Throw<ArgumentNullException>();
    }

    private static DietProfile BuildProfile()
    {
        return new DietProfile
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Rules =
            [
                new DietRule { Category = MealCategory.Fish, MinPerWeek = 2, MaxPerWeek = 3, Description = "Fisk 2-3" },
                new DietRule { Category = MealCategory.Meat, MinPerWeek = 0, MaxPerWeek = 3, Description = "Kött 0-3" },
                new DietRule { Category = MealCategory.Vegetarian, MinPerWeek = 2, MaxPerWeek = 7, Description = "Veg 2+" }
            ]
        };
    }

    private static WeeklyPlan BuildPlan(params (DayOfWeek Day, MealCategory Category)[] assignments)
    {
        return new WeeklyPlan
        {
            Id = Guid.NewGuid(),
            Year = 2026,
            WeekNumber = 16,
            Days = assignments.Select(a => new DayPlan
            {
                Id = Guid.NewGuid(),
                DayOfWeek = a.Day,
                Meal = new Meal
                {
                    Id = Guid.NewGuid(),
                    Name = $"Meal-{a.Category}",
                    Category = a.Category
                }
            }).ToList()
        };
    }
}
