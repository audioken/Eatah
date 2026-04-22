using System.Net;
using System.Net.Http.Json;
using Eatah.Api.Features.DietRules;
using Eatah.Api.Features.WeeklyPlan;

namespace Eatah.Api.Tests.Integration;

public class DietRuleEndpointsTests : IClassFixture<EatahWebApplicationFactory>
{
    private readonly EatahWebApplicationFactory _factory;

    public DietRuleEndpointsTests(EatahWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAllProfiles_ShouldReturnSeededProfile()
    {
        var client = _factory.CreateClient();

        var profiles = await client.GetFromJsonAsync<List<DietProfileResponse>>("api/dietprofiles");

        profiles.Should().NotBeNull();
        profiles!.Should().Contain(p => p.Name == "Livsmedelsverket");
    }

    [Fact]
    public async Task EvaluateWeeklyPlan_ShouldReturnEvaluation()
    {
        var client = _factory.CreateClient();
        var plan = await client.GetFromJsonAsync<WeeklyPlanResponse>("api/weeklyplans/current");
        var profiles = await client.GetFromJsonAsync<List<DietProfileResponse>>("api/dietprofiles");
        var profileId = profiles!.First().Id;

        // Randomize to get some meals assigned
        await client.PostAsJsonAsync(
            $"api/weeklyplans/{plan!.Id}/randomize",
            new RandomizeWeeklyPlanRequest(null));

        var response = await client.PostAsync(
            $"api/weeklyplans/{plan.Id}/evaluate?profileId={profileId}",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var evaluation = await response.Content.ReadFromJsonAsync<DietEvaluationResponse>();
        evaluation!.RuleResults.Should().NotBeEmpty();
        evaluation.OverallScore.Should().BeInRange(0.0, 1.0);
    }

    [Fact]
    public async Task EvaluateWeeklyPlan_ShouldReturn404_WhenProfileDoesNotExist()
    {
        var client = _factory.CreateClient();
        var plan = await client.GetFromJsonAsync<WeeklyPlanResponse>("api/weeklyplans/current");

        var response = await client.PostAsync(
            $"api/weeklyplans/{plan!.Id}/evaluate?profileId={Guid.NewGuid()}",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
