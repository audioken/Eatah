using Eatah.Api.Common;
using Eatah.Domain.Entities;
using Eatah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Eatah.Api.Features.Pantry;

public class IngredientCatalogService
{
    private readonly EatahDbContext _db;
    private readonly IWorkspaceContext _ws;

    public IngredientCatalogService(EatahDbContext db, IWorkspaceContext ws)
    {
        _db = db;
        _ws = ws;
    }

    public async Task<List<IngredientResponse>> SearchAsync(string? query, CancellationToken ct)
    {
        var wsId = _ws.CurrentWorkspaceId;
        var q = _db.IngredientMaster.AsNoTracking()
            .Where(i => i.WorkspaceId == null || i.WorkspaceId == wsId);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var lower = query.Trim().ToLower();
            q = q.Where(i => i.Name.ToLower().Contains(lower));
        }
        return await q.OrderBy(i => i.Name).Take(50)
            .Select(i => new IngredientResponse(i.Id, i.Name, i.Category, i.WorkspaceId == null))
            .ToListAsync(ct);
    }

    public async Task<Result<IngredientResponse>> CreateAsync(string name, string? category, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.BadRequest(ErrorCodes.IngredientNameRequired, "Ingredient name is required.");
        var wsId = _ws.RequireCurrent();
        var trimmed = name.Trim();
        var exists = await _db.IngredientMaster.AnyAsync(i => i.WorkspaceId == wsId && i.Name == trimmed, ct);
        if (exists)
            return Error.Conflict(ErrorCodes.IngredientNameRequired, "Ingredient already exists in this workspace.");
        var entity = new IngredientMaster { Id = Guid.NewGuid(), Name = trimmed, Category = category, WorkspaceId = wsId };
        _db.IngredientMaster.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new IngredientResponse(entity.Id, entity.Name, entity.Category, false);
    }
}

public class PantryService
{
    private readonly EatahDbContext _db;
    private readonly IWorkspaceContext _ws;

    public PantryService(EatahDbContext db, IWorkspaceContext ws)
    {
        _db = db;
        _ws = ws;
    }

    public async Task<List<PantryItemResponse>> GetAllAsync(CancellationToken ct)
    {
        var wsId = _ws.RequireCurrent();
        return await _db.PantryItems.AsNoTracking()
            .Include(p => p.Ingredient)
            .Where(p => p.WorkspaceId == wsId)
            .OrderByDescending(p => p.AddedAt)
            .Select(p => new PantryItemResponse(p.Id, p.IngredientId, p.Ingredient!.Name, p.Ingredient.Category, p.AddedAt))
            .ToListAsync(ct);
    }

    public async Task<Result<PantryItemResponse>> AddAsync(Guid ingredientId, CancellationToken ct)
    {
        var wsId = _ws.RequireCurrent();
        var ing = await _db.IngredientMaster
            .FirstOrDefaultAsync(i => i.Id == ingredientId && (i.WorkspaceId == null || i.WorkspaceId == wsId), ct);
        if (ing is null) return Error.NotFound(ErrorCodes.IngredientNotFound, "Ingredient not found.");
        var existing = await _db.PantryItems.FirstOrDefaultAsync(p => p.WorkspaceId == wsId && p.IngredientId == ingredientId, ct);
        if (existing is not null)
            return Error.Conflict(ErrorCodes.PantryItemAlreadyExists, "Ingredient already in pantry.");
        var entity = new PantryItem
        {
            Id = Guid.NewGuid(),
            WorkspaceId = wsId,
            IngredientId = ingredientId,
            AddedAt = DateTime.UtcNow
        };
        _db.PantryItems.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new PantryItemResponse(entity.Id, ing.Id, ing.Name, ing.Category, entity.AddedAt);
    }

    public async Task<Result> RemoveAsync(Guid id, CancellationToken ct)
    {
        var wsId = _ws.RequireCurrent();
        var entity = await _db.PantryItems.FirstOrDefaultAsync(p => p.Id == id && p.WorkspaceId == wsId, ct);
        if (entity is null) return Error.NotFound(ErrorCodes.PantryItemNotFound, "Pantry item not found.");
        _db.PantryItems.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class ShoppingListService
{
    private readonly EatahDbContext _db;
    private readonly IWorkspaceContext _ws;

    public ShoppingListService(EatahDbContext db, IWorkspaceContext ws)
    {
        _db = db;
        _ws = ws;
    }

    public async Task<List<ShoppingItemResponse>> GetAllAsync(CancellationToken ct)
    {
        var wsId = _ws.RequireCurrent();
        return await _db.ShoppingItems.AsNoTracking()
            .Include(s => s.Ingredient)
            .Where(s => s.WorkspaceId == wsId)
            .OrderBy(s => s.IsChecked).ThenByDescending(s => s.AddedAt)
            .Select(s => new ShoppingItemResponse(s.Id, s.IngredientId, s.Ingredient!.Name, s.Ingredient.Category, s.IsChecked, s.AddedAt))
            .ToListAsync(ct);
    }

    public async Task<Result<ShoppingItemResponse>> AddAsync(Guid ingredientId, CancellationToken ct)
    {
        var wsId = _ws.RequireCurrent();
        var ing = await _db.IngredientMaster
            .FirstOrDefaultAsync(i => i.Id == ingredientId && (i.WorkspaceId == null || i.WorkspaceId == wsId), ct);
        if (ing is null) return Error.NotFound(ErrorCodes.IngredientNotFound, "Ingredient not found.");
        var existing = await _db.ShoppingItems.FirstOrDefaultAsync(s => s.WorkspaceId == wsId && s.IngredientId == ingredientId, ct);
        if (existing is not null)
        {
            if (existing.IsChecked) { existing.IsChecked = false; existing.AddedAt = DateTime.UtcNow; await _db.SaveChangesAsync(ct); }
            return new ShoppingItemResponse(existing.Id, ing.Id, ing.Name, ing.Category, existing.IsChecked, existing.AddedAt);
        }
        var entity = new ShoppingItem
        {
            Id = Guid.NewGuid(),
            WorkspaceId = wsId,
            IngredientId = ingredientId,
            IsChecked = false,
            AddedAt = DateTime.UtcNow
        };
        _db.ShoppingItems.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new ShoppingItemResponse(entity.Id, ing.Id, ing.Name, ing.Category, false, entity.AddedAt);
    }

    public async Task<Result> ToggleAsync(Guid id, bool isChecked, CancellationToken ct)
    {
        var wsId = _ws.RequireCurrent();
        var entity = await _db.ShoppingItems.FirstOrDefaultAsync(s => s.Id == id && s.WorkspaceId == wsId, ct);
        if (entity is null) return Error.NotFound(ErrorCodes.ShoppingItemNotFound, "Shopping item not found.");
        entity.IsChecked = isChecked;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RemoveAsync(Guid id, CancellationToken ct)
    {
        var wsId = _ws.RequireCurrent();
        var entity = await _db.ShoppingItems.FirstOrDefaultAsync(s => s.Id == id && s.WorkspaceId == wsId, ct);
        if (entity is null) return Error.NotFound(ErrorCodes.ShoppingItemNotFound, "Shopping item not found.");
        _db.ShoppingItems.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task ClearCheckedAsync(CancellationToken ct)
    {
        var wsId = _ws.RequireCurrent();
        await _db.ShoppingItems
            .Where(s => s.WorkspaceId == wsId && s.IsChecked)
            .ExecuteDeleteAsync(ct);
    }
}
