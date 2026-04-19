using Eatah.Domain.Entities;
using Eatah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Eatah.Api.Features.Meals;

public interface IMealRepository
{
    Task<List<Meal>> GetAllAsync(CancellationToken cancellationToken);
    Task<Meal?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Meal meal, CancellationToken cancellationToken);
    Task UpdateAsync(Meal meal, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public class MealRepository : IMealRepository
{
    private readonly EatahDbContext _context;

    public MealRepository(EatahDbContext context)
    {
        _context = context;
    }

    public async Task<List<Meal>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Meals
            .Include(m => m.Ingredients)
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Meal?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Meals
            .Include(m => m.Ingredients)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task AddAsync(Meal meal, CancellationToken cancellationToken)
    {
        await _context.Meals.AddAsync(meal, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Meal meal, CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var meal = await _context.Meals
            .Include(m => m.Ingredients)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (meal is null)
        {
            return false;
        }

        _context.Meals.Remove(meal);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
