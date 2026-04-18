using Eatah.Api.Features.DietRules;
using Eatah.Api.Features.WeeklyPlan;
using Eatah.Domain.Entities;

namespace Eatah.Api.Tests.Unit;

public class RandomMealGeneratorTests
{
    private static readonly DayOfWeek[] Week =
    [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    ];

    private readonly RandomMealGenerator _sut = new(new DietRuleEvaluator());

    [Fact]
    public void Generate_ShouldReturnEmptySlots_WhenNoMealsAvailable()
    {
        var result = _sut.Generate(Array.Empty<Meal>(), Week, null, 0.0);

        result.Should().HaveCount(7);
        result.Should().OnlyContain(m => m == null);
    }

    [Fact]
    public void Generate_ShouldNotRepeatMeals_WhenEnoughMealsAvailable()
    {
        var meals = Enumerable.Range(0, 10)
            .Select(i => new Meal { Id = Guid.NewGuid(), Name = $"M{i}", Category = MealCategory.Vegetarian })
            .ToList();

        var result = _sut.Generate(meals, Week, null, 0.0);

        result.Should().HaveCount(7);
        result.Select(m => m!.Id).Distinct().Should().HaveCount(7);
    }

    [Fact]
    public void Generate_WithHighStrictness_ShouldProducePlanThatSatisfiesProfile()
    {
        var meals = new List<Meal>
        {
            new() { Id = Guid.NewGuid(), Name = "Fisk 1", Category = MealCategory.Fish },
            new() { Id = Guid.NewGuid(), Name = "Fisk 2", Category = MealCategory.Fish },
            new() { Id = Guid.NewGuid(), Name = "Fisk 3", Category = MealCategory.Fish },
            new() { Id = Guid.NewGuid(), Name = "Kött 1", Category = MealCategory.Meat },
            new() { Id = Guid.NewGuid(), Name = "Kött 2", Category = MealCategory.Meat },
            new() { Id = Guid.NewGuid(), Name = "Veg 1", Category = MealCategory.Vegetarian },
            new() { Id = Guid.NewGuid(), Name = "Veg 2", Category = MealCategory.Vegetarian },
            new() { Id = Guid.NewGuid(), Name = "Veg 3", Category = MealCategory.Vegetarian },
            new() { Id = Guid.NewGuid(), Name = "Vegan 1", Category = MealCategory.Vegan }
        };

        var profile = new DietProfile
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Rules =
            [
                new DietRule { Category = MealCategory.Fish, MinPerWeek = 2, MaxPerWeek = 3, Description = "" },
                new DietRule { Category = MealCategory.Meat, MinPerWeek = 0, MaxPerWeek = 3, Description = "" },
                new DietRule { Category = MealCategory.Vegetarian, MinPerWeek = 2, MaxPerWeek = 7, Description = "" }
            ]
        };

        var result = _sut.Generate(meals, Week, profile, 1.0);
        var evaluator = new DietRuleEvaluator();

        var plan = new WeeklyPlan
        {
            Days = result.Select((m, i) => new DayPlan
            {
                Id = Guid.NewGuid(),
                DayOfWeek = Week[i],
                MealId = m?.Id,
                Meal = m
            }).ToList()
        };

        evaluator.Evaluate(plan, profile).OverallScore.Should().Be(1.0);
    }

    [Fact]
    public void Generate_WithZeroStrictness_ShouldIgnoreProfile()
    {
        var meals = new List<Meal>
        {
            new() { Id = Guid.NewGuid(), Name = "M", Category = MealCategory.Meat }
        };
        var profile = new DietProfile
        {
            Id = Guid.NewGuid(),
            Name = "x",
            Rules = [new DietRule { Category = MealCategory.Fish, MinPerWeek = 5, MaxPerWeek = 7, Description = "" }]
        };

        var result = _sut.Generate(meals, Week, profile, 0.0);

        result.Should().HaveCount(7);
        result.Should().OnlyContain(m => m != null);
    }
}
