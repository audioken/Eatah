using Eatah.Api.Common;
using Eatah.Domain.Entities;
using Eatah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Eatah.Api.Features.Meals;

public interface IMealRepository
{
    Task<List<Meal>> GetAllAsync(CancellationToken cancellationToken);
    Task<Meal?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Meal meal, CancellationToken cancellationToken);
    Task ReplaceIngredientsAndUpdateAsync(Meal meal, IReadOnlyCollection<Ingredient> newIngredients, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public class MealRepository : IMealRepository
{
    private readonly EatahDbContext _context;
    private readonly IWorkspaceContext _workspace;

    public MealRepository(EatahDbContext context, IWorkspaceContext workspace)
    {
        _context = context;
        _workspace = workspace;
    }

    /// <summary>System meals (WorkspaceId null) + meals belonging to the current workspace.</summary>
    public async Task<List<Meal>> GetAllAsync(CancellationToken cancellationToken)
    {
        var wsId = _workspace.CurrentWorkspaceId;
        return await _context.Meals
            .Include(m => m.Ingredients)
            .AsNoTracking()
            .Where(m => m.WorkspaceId == null || m.WorkspaceId == wsId)
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Meal?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var wsId = _workspace.CurrentWorkspaceId;
        return await _context.Meals
            .Include(m => m.Ingredients)
            .Where(m => m.WorkspaceId == null || m.WorkspaceId == wsId)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task AddAsync(Meal meal, CancellationToken cancellationToken)
    {
        meal.WorkspaceId ??= _workspace.RequireCurrent();
        await _context.Meals.AddAsync(meal, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceIngredientsAndUpdateAsync(
        Meal meal,
        IReadOnlyCollection<Ingredient> newIngredients,
        CancellationToken cancellationToken)
    {
        _context.Ingredients.RemoveRange(meal.Ingredients);
        meal.Ingredients = [.. newIngredients];
        await _context.Ingredients.AddRangeAsync(newIngredients, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Only deletes workspace-owned meals; system meals are protected.</summary>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var wsId = _workspace.CurrentWorkspaceId;
        var meal = await _context.Meals
            .Include(m => m.Ingredients)
            .FirstOrDefaultAsync(m => m.Id == id && m.WorkspaceId == wsId, cancellationToken);

        if (meal is null) return false;

        _context.Meals.Remove(meal);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
