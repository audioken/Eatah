using Eatah.Domain.Entities;
using Eatah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Eatah.Api.Features.WeeklyPlan;

public interface IWeeklyPlanRepository
{
    Task<Eatah.Domain.Entities.WeeklyPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Eatah.Domain.Entities.WeeklyPlan?> GetByYearWeekAsync(int year, int weekNumber, CancellationToken cancellationToken);
    Task AddAsync(Eatah.Domain.Entities.WeeklyPlan plan, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public class WeeklyPlanRepository : IWeeklyPlanRepository
{
    private readonly EatahDbContext _context;

    public WeeklyPlanRepository(EatahDbContext context)
    {
        _context = context;
    }

    public async Task<Eatah.Domain.Entities.WeeklyPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.WeeklyPlans
            .Include(w => w.Days)
                .ThenInclude(d => d.Meal)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<Eatah.Domain.Entities.WeeklyPlan?> GetByYearWeekAsync(int year, int weekNumber, CancellationToken cancellationToken)
    {
        return await _context.WeeklyPlans
            .Include(w => w.Days)
                .ThenInclude(d => d.Meal)
            .FirstOrDefaultAsync(w => w.Year == year && w.WeekNumber == weekNumber, cancellationToken);
    }

    public async Task AddAsync(Eatah.Domain.Entities.WeeklyPlan plan, CancellationToken cancellationToken)
    {
        await _context.WeeklyPlans.AddAsync(plan, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
