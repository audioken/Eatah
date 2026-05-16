using Eatah.Api.Common;
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
    private readonly IWorkspaceContext _workspace;

    public WeeklyPlanRepository(EatahDbContext context, IWorkspaceContext workspace)
    {
        _context = context;
        _workspace = workspace;
    }

    public async Task<Eatah.Domain.Entities.WeeklyPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var wsId = _workspace.RequireCurrent();
        return await _context.WeeklyPlans
            .Include(w => w.Days)
                .ThenInclude(d => d.Meal)
            .FirstOrDefaultAsync(w => w.Id == id && w.WorkspaceId == wsId, cancellationToken);
    }

    public async Task<Eatah.Domain.Entities.WeeklyPlan?> GetByYearWeekAsync(int year, int weekNumber, CancellationToken cancellationToken)
    {
        var wsId = _workspace.RequireCurrent();
        return await _context.WeeklyPlans
            .Include(w => w.Days)
                .ThenInclude(d => d.Meal)
            .FirstOrDefaultAsync(w => w.WorkspaceId == wsId && w.Year == year && w.WeekNumber == weekNumber, cancellationToken);
    }

    public async Task AddAsync(Eatah.Domain.Entities.WeeklyPlan plan, CancellationToken cancellationToken)
    {
        if (plan.WorkspaceId == Guid.Empty) plan.WorkspaceId = _workspace.RequireCurrent();
        await _context.WeeklyPlans.AddAsync(plan, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
