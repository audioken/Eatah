using Eatah.Api.Common;
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
            ["  Kyckling  ", "Ris"],
            null);

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Kycklinggryta");
        result.Value.Ingredients.Should().HaveCount(2);
        result.Value.Ingredients[0].Name.Should().Be("Kyckling");
        _repo.Verify(r => r.AddAsync(It.Is<Meal>(m => m.Name == "Kycklinggryta"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNotFoundError_WhenMealDoesNotExist()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Meal?)null);
        var request = new UpdateMealRequest("X", MealCategory.Fish, ["Lax"], null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), request, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCodes.MealNotFound);
        result.Error.StatusCode.Should().Be(404);
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
            CookingTimeMinutes = 10,
            Ingredients = [new Ingredient { Id = Guid.NewGuid(), Name = "Old" }]
        };
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _sut.UpdateAsync(id, new UpdateMealRequest("Nytt", MealCategory.Fish, ["Lax"], 25), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Nytt");
        result.Value.Category.Should().Be(MealCategory.Fish);
        result.Value.CookingTimeMinutes.Should().Be(25);
        result.Value.Ingredients.Should().ContainSingle(i => i.Name == "Lax");
        _repo.Verify(r => r.ReplaceIngredientsAndUpdateAsync(
            existing,
            It.Is<IReadOnlyCollection<Ingredient>>(list => list.Count == 1 && list.Single().Name == "Lax"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnSuccess_WhenRepositoryDeletes()
    {
        _repo.Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _sut.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnNotFoundError_WhenRepositoryReturnsFalse()
    {
        _repo.Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _sut.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCodes.MealNotFound);
    }
}
