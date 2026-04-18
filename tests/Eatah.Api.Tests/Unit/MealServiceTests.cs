using Eatah.Api.Features.Meals;
using Eatah.Domain.Entities;
using Moq;

namespace Eatah.Api.Tests.Unit;

public class MealServiceTests
{
    private readonly Mock<IMealRepository> _repo = new();
    private readonly MealService _sut;

    public MealServiceTests()
    {
        _sut = new MealService(_repo.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnMappedMeals()
    {
        var meal = new Meal
        {
            Id = Guid.NewGuid(),
            Name = "Pasta",
            Category = MealCategory.Vegetarian,
            Ingredients = [new Ingredient { Id = Guid.NewGuid(), Name = "Tomat" }]
        };
        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([meal]);

        var result = await _sut.GetAllAsync(CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Pasta");
        result[0].Ingredients.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateAsync_ShouldTrimAndPersistNewMeal()
    {
        var request = new CreateMealRequest(
            "  Kycklinggryta  ",
            MealCategory.Meat,
            ["  Kyckling  ", "Ris"]);

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.Name.Should().Be("Kycklinggryta");
        result.Ingredients.Should().HaveCount(2);
        result.Ingredients[0].Name.Should().Be("Kyckling");
        _repo.Verify(r => r.AddAsync(It.Is<Meal>(m => m.Name == "Kycklinggryta"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenMealNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Meal?)null);
        var request = new UpdateMealRequest("X", MealCategory.Fish, ["Lax"]);

        Func<Task> act = () => _sut.UpdateAsync(Guid.NewGuid(), request, CancellationToken.None);

        await act.Should().ThrowAsync<MealNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_ShouldReplaceFieldsAndIngredients()
    {
        var id = Guid.NewGuid();
        var existing = new Meal
        {
            Id = id,
            Name = "Gammal",
            Category = MealCategory.Meat,
            Ingredients = [new Ingredient { Id = Guid.NewGuid(), Name = "Old" }]
        };
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _sut.UpdateAsync(id, new UpdateMealRequest("Nytt", MealCategory.Fish, ["Lax"]), CancellationToken.None);

        result.Name.Should().Be("Nytt");
        result.Category.Should().Be(MealCategory.Fish);
        result.Ingredients.Should().ContainSingle(i => i.Name == "Lax");
        _repo.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnRepositoryResult()
    {
        _repo.Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        (await _sut.DeleteAsync(Guid.NewGuid(), CancellationToken.None)).Should().BeTrue();
    }
}
