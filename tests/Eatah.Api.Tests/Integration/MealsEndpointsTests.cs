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
        var request = new CreateMealRequest("Testrätt", MealCategory.Vegan, ["A", "B"], null);

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
        var request = new CreateMealRequest("", MealCategory.Meat, ["A"], null);

        var response = await client.PostAsJsonAsync("api/meals", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_ShouldReturn404_WhenMealDoesNotExist()
    {
        var client = _factory.CreateClient();
        var request = new UpdateMealRequest("N", MealCategory.Fish, ["Lax"], null);

        var response = await client.PutAsJsonAsync($"api/meals/{Guid.NewGuid()}", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ShouldReturn200_AndPersist_WhenAddingIngredientToExistingMeal()
    {
        var client = _factory.CreateClient();
        var createdResponse = await client.PostAsJsonAsync("api/meals",
            new CreateMealRequest("Pasta", MealCategory.Vegetarian, ["Tomat"], 20));
        createdResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createdResponse.Content.ReadFromJsonAsync<MealResponse>();

        var updateRequest = new UpdateMealRequest("Pasta Deluxe", MealCategory.Vegetarian, ["Tomat", "Basilika"], 30);
        var updateResponse = await client.PutAsJsonAsync($"api/meals/{created!.Id}", updateRequest);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<MealResponse>();
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Pasta Deluxe");
        updated.CookingTimeMinutes.Should().Be(30);
        updated.Ingredients.Should().HaveCount(2);
        updated.Ingredients.Select(i => i.Name).Should().BeEquivalentTo(["Tomat", "Basilika"]);

        var persisted = await client.GetFromJsonAsync<MealResponse>($"api/meals/{created.Id}");
        persisted.Should().NotBeNull();
        persisted!.Ingredients.Should().HaveCount(2);
        persisted.Ingredients.Select(i => i.Name).Should().BeEquivalentTo(["Tomat", "Basilika"]);
        persisted.CookingTimeMinutes.Should().Be(30);
    }

    [Fact]
    public async Task Delete_ShouldRemoveMeal()
    {
        var client = _factory.CreateClient();
        var created = await client.PostAsJsonAsync("api/meals",
            new CreateMealRequest("Ta bort mig", MealCategory.Meat, ["x"], null));
        var meal = await created.Content.ReadFromJsonAsync<MealResponse>();

        var response = await client.DeleteAsync($"api/meals/{meal!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var check = await client.GetAsync($"api/meals/{meal.Id}");
        check.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
