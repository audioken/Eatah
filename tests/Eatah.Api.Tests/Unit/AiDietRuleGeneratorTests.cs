using Eatah.Api.Features.AI;
using Eatah.Domain.Entities;

namespace Eatah.Api.Tests.Unit;

public class AiDietRuleGeneratorTests
{
    [Fact]
    public void ParseAndValidate_ShouldParseValidJson()
    {
        var json = """
            {
              "name": "LCHF",
              "rules": [
                { "category": "Meat", "minPerWeek": 3, "maxPerWeek": 5, "description": "Kött varje dag" },
                { "category": "Fish", "minPerWeek": 1, "maxPerWeek": 2, "description": "Fisk sparsamt" }
              ]
            }
            """;

        var result = AiDietRuleGenerator.ParseAndValidate(json, "fallback");

        result.Name.Should().Be("LCHF");
        result.Rules.Should().HaveCount(2);
        result.Rules.Should().Contain(r => r.Category == MealCategory.Meat && r.MinPerWeek == 3);
    }

    [Fact]
    public void ParseAndValidate_ShouldSwapMinMax_WhenInverted()
    {
        var json = """
            { "name": "X", "rules": [ { "category": "Meat", "minPerWeek": 5, "maxPerWeek": 2, "description": "" } ] }
            """;

        var result = AiDietRuleGenerator.ParseAndValidate(json, "fallback");

        result.Rules[0].MinPerWeek.Should().Be(2);
        result.Rules[0].MaxPerWeek.Should().Be(5);
    }

    [Fact]
    public void ParseAndValidate_ShouldDropDuplicateCategories()
    {
        var json = """
            {
              "name": "Dup",
              "rules": [
                { "category": "Meat", "minPerWeek": 1, "maxPerWeek": 3, "description": "a" },
                { "category": "Meat", "minPerWeek": 2, "maxPerWeek": 4, "description": "b" }
              ]
            }
            """;

        var result = AiDietRuleGenerator.ParseAndValidate(json, "fallback");

        result.Rules.Should().HaveCount(1);
    }

    [Fact]
    public void ParseAndValidate_ShouldThrow_WhenJsonIsInvalid()
    {
        Action act = () => AiDietRuleGenerator.ParseAndValidate("not json", "fallback");
        act.Should().Throw<AiServiceException>();
    }

    [Fact]
    public void ParseAndValidate_ShouldThrow_WhenNoValidRules()
    {
        var json = """{ "name": "x", "rules": [] }""";
        Action act = () => AiDietRuleGenerator.ParseAndValidate(json, "fallback");
        act.Should().Throw<AiServiceException>();
    }

    [Fact]
    public void ParseAndValidate_ShouldUseFallbackName_WhenMissing()
    {
        var json = """{ "rules": [ { "category": "Fish", "minPerWeek": 2, "maxPerWeek": 3, "description": "F" } ] }""";
        var result = AiDietRuleGenerator.ParseAndValidate(json, "Fallback");
        result.Name.Should().Be("Fallback");
    }
}
