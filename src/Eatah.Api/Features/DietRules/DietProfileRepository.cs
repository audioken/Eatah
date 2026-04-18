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
}

public class DietProfileRepository : IDietProfileRepository
{
    private readonly EatahDbContext _context;

    public DietProfileRepository(EatahDbContext context)
    {
        _context = context;
    }

    public async Task<List<DietProfile>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.DietProfiles
            .Include(p => p.Rules)
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<DietProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.DietProfiles
            .Include(p => p.Rules)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken)
    {
        return await _context.DietProfiles
            .AsNoTracking()
            .AnyAsync(p => p.Name == name, cancellationToken);
    }

    public async Task AddAsync(DietProfile profile, CancellationToken cancellationToken)
    {
        await _context.DietProfiles.AddAsync(profile, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
