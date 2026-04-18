using Eatah.Domain.Entities;

namespace Eatah.Api.Features.Meals;

public class MealNotFoundException : Exception
{
    public MealNotFoundException(Guid id)
        : base($"Maträtt med id {id} hittades inte.")
    {
    }
}

public class MealService
{
    private readonly IMealRepository _repository;

    public MealService(IMealRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<MealResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var meals = await _repository.GetAllAsync(cancellationToken);
        return meals.Select(ToResponse).ToList();
    }

    public async Task<MealResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var meal = await _repository.GetByIdAsync(id, cancellationToken);
        return meal is null ? null : ToResponse(meal);
    }

    public async Task<MealResponse> CreateAsync(CreateMealRequest request, CancellationToken cancellationToken)
    {
        var meal = new Meal
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Category = request.Category,
            CookingTimeMinutes = request.CookingTimeMinutes,
            Ingredients = request.Ingredients
                .Select(name => new Ingredient { Id = Guid.NewGuid(), Name = name.Trim() })
                .ToList()
        };

        await _repository.AddAsync(meal, cancellationToken);
        return ToResponse(meal);
    }

    public async Task<MealResponse> UpdateAsync(Guid id, UpdateMealRequest request, CancellationToken cancellationToken)
    {
        var meal = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new MealNotFoundException(id);

        meal.Name = request.Name.Trim();
        meal.Category = request.Category;
        meal.Ingredients.Clear();
        foreach (var name in request.Ingredients)
        {
            meal.Ingredients.Add(new Ingredient { Id = Guid.NewGuid(), Name = name.Trim() });
        }

        await _repository.UpdateAsync(meal, cancellationToken);
        return ToResponse(meal);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _repository.DeleteAsync(id, cancellationToken);
    }

    private static MealResponse ToResponse(Meal meal)
    {
        return new MealResponse(
            meal.Id,
            meal.Name,
            meal.Category,
            meal.CookingTimeMinutes,
            meal.CreatedAt,
            meal.Ingredients.Select(i => new IngredientDto(i.Id, i.Name)).ToList());
    }
}
