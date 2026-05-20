using Eatah.Api.Common;
using Eatah.Domain.Entities;
using Eatah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Eatah.Api.Features.DietRules;

public interface IDietProfileRepository
{
    Task<List<DietProfile>> GetAllAsync(CancellationToken cancellationToken);
    Task<DietProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);
    Task AddAsync(DietProfile profile, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public class DietProfileRepository : IDietProfileRepository
{
    private readonly EatahDbContext _context;
    private readonly IWorkspaceContext _workspace;

    public DietProfileRepository(EatahDbContext context, IWorkspaceContext workspace)
    {
        _context = context;
        _workspace = workspace;
    }

    /// <summary>
    /// System profiles (WorkspaceId null) + profiles belonging to the current household.
    /// </summary>
    public async Task<List<DietProfile>> GetAllAsync(CancellationToken cancellationToken)
    {
        var wsId = _workspace.CurrentWorkspaceId;
        return await _context.DietProfiles
            .Include(p => p.Rules)
            .AsNoTracking()
            .Where(p => p.WorkspaceId == null || (wsId.HasValue && p.WorkspaceId == wsId))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<DietProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var wsId = _workspace.CurrentWorkspaceId;
        return await _context.DietProfiles
            .Include(p => p.Rules)
            .AsNoTracking()
            .Where(p => p.WorkspaceId == null || p.WorkspaceId == wsId)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken)
    {
        var wsId = _workspace.CurrentWorkspaceId;
        return await _context.DietProfiles
            .AsNoTracking()
            .AnyAsync(p => p.Name == name && (p.WorkspaceId == null || p.WorkspaceId == wsId), cancellationToken);
    }

    public async Task AddAsync(DietProfile profile, CancellationToken cancellationToken)
    {
        profile.WorkspaceId ??= _workspace.RequireCurrent();
        await _context.DietProfiles.AddAsync(profile, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Deletes a workspace-owned profile. System profiles (WorkspaceId null) cannot be deleted.</summary>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var wsId = _workspace.RequireCurrent();
        var deleted = await _context.DietProfiles
            .Where(p => p.Id == id && p.WorkspaceId == wsId)
            .ExecuteDeleteAsync(cancellationToken);
        return deleted > 0;
    }
}
