using System.Net;
using System.Net.Http.Json;
using Eatah.Api.Features.Meals;
using Eatah.Domain.Entities;

namespace Eatah.Api.Tests.Integration;

public class MealsEndpointsTests : IClassFixture<EatahWebApplicationFactory>
{
    private readonly EatahWebApplicationFactory _factory;

    public MealsEndpointsTests(EatahWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_ShouldReturnSeededMeals()
    {
        var client = _factory.CreateClient();

        var meals = await client.GetFromJsonAsync<List<MealResponse>>("api/meals");

        meals.Should().NotBeNull();
        meals!.Count.Should().BeGreaterOrEqualTo(10);
    }

    [Fact]
    public async Task Create_ShouldReturnCreated_AndPersist()
    {
        var client = _factory.CreateClient();
        var request = new CreateMealRequest("Testrätt", MealCategory.Vegan, ["A", "B"]);

        var response = await client.PostAsJsonAsync("api/meals", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<MealResponse>();
        created!.Name.Should().Be("Testrätt");
        created.Ingredients.Should().HaveCount(2);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenNameIsEmpty()
    {
        var client = _factory.CreateClient();
        var request = new CreateMealRequest("", MealCategory.Meat, ["A"]);

        var response = await client.PostAsJsonAsync("api/meals", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_ShouldReturn404_WhenMealDoesNotExist()
    {
        var client = _factory.CreateClient();
        var request = new UpdateMealRequest("N", MealCategory.Fish, ["Lax"]);

        var response = await client.PutAsJsonAsync($"api/meals/{Guid.NewGuid()}", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldRemoveMeal()
    {
        var client = _factory.CreateClient();
        var created = await client.PostAsJsonAsync("api/meals",
            new CreateMealRequest("Ta bort mig", MealCategory.Meat, ["x"]));
        var meal = await created.Content.ReadFromJsonAsync<MealResponse>();

        var response = await client.DeleteAsync($"api/meals/{meal!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var check = await client.GetAsync($"api/meals/{meal.Id}");
        check.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
