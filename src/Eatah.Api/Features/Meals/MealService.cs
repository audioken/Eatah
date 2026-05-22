using Eatah.Api.Common;
using Eatah.Domain.Entities;

namespace Eatah.Api.Features.Meals;

public class MealService
{
    private readonly IMealRepository _repository;
    private readonly IRealtimeNotifier _notifier;
    private readonly IWorkspaceContext _workspace;

    public MealService(IMealRepository repository, IRealtimeNotifier notifier, IWorkspaceContext workspace)
    {
        _repository = repository;
        _notifier = notifier;
        _workspace = workspace;
    }

    public async Task<List<MealResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var meals = await _repository.GetAllAsync(cancellationToken);
        return meals.Select(ToResponse).ToList();
    }

    public async Task<Result<MealResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var meal = await _repository.GetByIdAsync(id, cancellationToken);
        return meal is null
            ? MealErrors.NotFound(id)
            : ToResponse(meal);
    }

    public async Task<Result<MealResponse>> CreateAsync(CreateMealRequest request, CancellationToken cancellationToken)
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
        await _notifier.MealsChangedAsync(_workspace.RequireCurrent(), cancellationToken);
        return ToResponse(meal);
    }

    public async Task<Result<MealResponse>> UpdateAsync(Guid id, UpdateMealRequest request, CancellationToken cancellationToken)
    {
        var meal = await _repository.GetByIdAsync(id, cancellationToken);
        if (meal is null)
        {
            return MealErrors.NotFound(id);
        }

        meal.Name = request.Name.Trim();
        meal.Category = request.Category;
        meal.CookingTimeMinutes = request.CookingTimeMinutes;

        var newIngredients = request.Ingredients
            .Select(name => new Ingredient { Id = Guid.NewGuid(), Name = name.Trim() })
            .ToList();

        await _repository.ReplaceIngredientsAndUpdateAsync(meal, newIngredients, cancellationToken);
        meal.Ingredients = newIngredients;
        await _notifier.MealsChangedAsync(_workspace.RequireCurrent(), cancellationToken);
        return ToResponse(meal);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _repository.DeleteAsync(id, cancellationToken);
        if (deleted)
        {
            await _notifier.MealsChangedAsync(_workspace.RequireCurrent(), cancellationToken);
        }
        return deleted ? Result.Success() : MealErrors.NotFound(id);
    }

    internal static MealResponse ToResponse(Meal meal)
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

internal static class MealErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound(ErrorCodes.MealNotFound, $"Meal with id {id} was not found.");
}
