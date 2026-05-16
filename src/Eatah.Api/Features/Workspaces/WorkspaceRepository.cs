using Eatah.Domain.Entities;
using Eatah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Eatah.Api.Features.Workspaces;

public interface IWorkspaceRepository
{
    Task<List<Workspace>> GetForUserAsync(Guid userId, CancellationToken ct);
    Task<Workspace?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<bool> HasHouseholdAsync(Guid userId, CancellationToken ct);
    Task AddAsync(Workspace workspace, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public class WorkspaceRepository : IWorkspaceRepository
{
    private readonly EatahDbContext _context;
    public WorkspaceRepository(EatahDbContext context) => _context = context;

    public async Task<List<Workspace>> GetForUserAsync(Guid userId, CancellationToken ct)
    {
        return await _context.Workspaces
            .Include(w => w.Members)
            .Where(w => w.Members.Any(m => m.UserId == userId))
            .OrderBy(w => w.Type)
            .ThenBy(w => w.Name)
            .ToListAsync(ct);
    }

    public async Task<Workspace?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.Workspaces
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Id == id, ct);
    }

    public async Task<bool> HasHouseholdAsync(Guid userId, CancellationToken ct)
    {
        return await _context.WorkspaceMembers
            .AnyAsync(m => m.UserId == userId && m.Workspace.Type == WorkspaceType.Household, ct);
    }

    public async Task AddAsync(Workspace workspace, CancellationToken ct)
    {
        await _context.Workspaces.AddAsync(workspace, ct);
        await _context.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => _context.SaveChangesAsync(ct);
}
