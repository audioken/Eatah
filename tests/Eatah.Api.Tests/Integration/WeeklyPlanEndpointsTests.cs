using System.Net;
using System.Net.Http.Json;
using Eatah.Api.Features.WeeklyPlan;

namespace Eatah.Api.Tests.Integration;

public class WeeklyPlanEndpointsTests : IClassFixture<EatahWebApplicationFactory>
{
    private readonly EatahWebApplicationFactory _factory;

    public WeeklyPlanEndpointsTests(EatahWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCurrent_ShouldReturnPlanWithSevenDays()
    {
        var client = _factory.CreateClient();

        var plan = await client.GetFromJsonAsync<WeeklyPlanResponse>("api/weeklyplans/current");

        plan.Should().NotBeNull();
        plan!.Days.Should().HaveCount(7);
    }

    [Fact]
    public async Task AssignMeal_ShouldPopulateDay()
    {
        var client = _factory.CreateClient();
        var plan = await client.GetFromJsonAsync<WeeklyPlanResponse>("api/weeklyplans/current");
        var meals = await client.GetFromJsonAsync<List<Eatah.Api.Features.Meals.MealResponse>>("api/meals");

        var response = await client.PutAsJsonAsync(
            $"api/weeklyplans/{plan!.Id}/days/{DayOfWeek.Monday}",
            new AssignMealRequest(meals![0].Id));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<WeeklyPlanResponse>();
        updated!.Days.Single(d => d.DayOfWeek == DayOfWeek.Monday).Meal!.Id.Should().Be(meals[0].Id);
    }

    [Fact]
    public async Task Randomize_ShouldAssignMealsToAllDays()
    {
        var client = _factory.CreateClient();
        var plan = await client.GetFromJsonAsync<WeeklyPlanResponse>("api/weeklyplans/current");

        var response = await client.PostAsJsonAsync(
            $"api/weeklyplans/{plan!.Id}/randomize",
            new RandomizeWeeklyPlanRequest(null, 0.0));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<WeeklyPlanResponse>();
        updated!.Days.Should().OnlyContain(d => d.Meal != null);
    }

    [Fact]
    public async Task ClearDay_ShouldRemoveMealFromDay()
    {
        var client = _factory.CreateClient();
        var plan = await client.GetFromJsonAsync<WeeklyPlanResponse>("api/weeklyplans/current");
        var meals = await client.GetFromJsonAsync<List<Eatah.Api.Features.Meals.MealResponse>>("api/meals");
        await client.PutAsJsonAsync(
            $"api/weeklyplans/{plan!.Id}/days/{DayOfWeek.Tuesday}",
            new AssignMealRequest(meals![0].Id));

        var response = await client.DeleteAsync($"api/weeklyplans/{plan.Id}/days/{DayOfWeek.Tuesday}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<WeeklyPlanResponse>();
        updated!.Days.Single(d => d.DayOfWeek == DayOfWeek.Tuesday).Meal.Should().BeNull();
    }
}
