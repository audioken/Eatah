using System.Globalization;
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
            .Select(s => new ShoppingItemResponse(s.Id, s.IngredientId, s.Ingredient!.Name, s.Ingredient.Category, s.IsChecked, s.AddedAt, s.Notes))
            .ToListAsync(ct);
    }

    public async Task<Result<ShoppingItemResponse>> AddAsync(Guid ingredientId, string? notes, CancellationToken ct)
    {
        var wsId = _ws.RequireCurrent();
        var ing = await _db.IngredientMaster
            .FirstOrDefaultAsync(i => i.Id == ingredientId && (i.WorkspaceId == null || i.WorkspaceId == wsId), ct);
        if (ing is null) return Error.NotFound(ErrorCodes.IngredientNotFound, "Ingredient not found.");
        var existing = await _db.ShoppingItems.FirstOrDefaultAsync(s => s.WorkspaceId == wsId && s.IngredientId == ingredientId, ct);
        if (existing is not null)
        {
            if (existing.IsChecked) { existing.IsChecked = false; existing.AddedAt = DateTime.UtcNow; }
            if (notes is not null) existing.Notes = notes;
            await _db.SaveChangesAsync(ct);
            return new ShoppingItemResponse(existing.Id, ing.Id, ing.Name, ing.Category, existing.IsChecked, existing.AddedAt, existing.Notes);
        }
        var entity = new ShoppingItem
        {
            Id = Guid.NewGuid(),
            WorkspaceId = wsId,
            IngredientId = ingredientId,
            IsChecked = false,
            AddedAt = DateTime.UtcNow,
            Notes = notes
        };
        _db.ShoppingItems.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new ShoppingItemResponse(entity.Id, ing.Id, ing.Name, ing.Category, false, entity.AddedAt, entity.Notes);
    }

    public async Task<Result> ToggleAsync(Guid id, bool isChecked, CancellationToken ct)
    {
        var wsId = _ws.RequireCurrent();
        var entity = await _db.ShoppingItems
            .Include(s => s.Ingredient)
            .FirstOrDefaultAsync(s => s.Id == id && s.WorkspaceId == wsId, ct);
        if (entity is null) return Error.NotFound(ErrorCodes.ShoppingItemNotFound, "Shopping item not found.");
        entity.IsChecked = isChecked;

        // Auto-move to pantry when checked
        if (isChecked)
        {
            var inPantry = await _db.PantryItems
                .AnyAsync(p => p.WorkspaceId == wsId && p.IngredientId == entity.IngredientId, ct);
            if (!inPantry)
            {
                _db.PantryItems.Add(new PantryItem
                {
                    Id = Guid.NewGuid(),
                    WorkspaceId = wsId,
                    IngredientId = entity.IngredientId,
                    AddedAt = DateTime.UtcNow
                });
            }
        }

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

    /// <summary>
    /// Syncs the shopping list with the given weekly plan: adds ingredients for currently
    /// planned meals (skipping pantry items) and removes any previously-synced items that
    /// are no longer needed. Manually-added items (Notes == null) are never removed.
    /// Returns the full updated list.
    /// </summary>
    public async Task<Result<List<ShoppingItemResponse>>> SyncFromWeeklyPlanAsync(Guid planId, CancellationToken ct)
    {
        var wsId = _ws.RequireCurrent();

        var plan = await _db.WeeklyPlans
            .AsNoTracking()
            .Include(p => p.Days)
                .ThenInclude(d => d.Meal)
                    .ThenInclude(m => m!.Ingredients)
            .FirstOrDefaultAsync(p => p.Id == planId && p.WorkspaceId == wsId, ct);

        if (plan is null)
            return Error.NotFound(ErrorCodes.WeeklyPlanNotFound, "Weekly plan not found.");

        var weekLabel = $"v{plan.WeekNumber}";

        // Load existing pantry ingredient IDs and all shopping items (tracked for mutations)
        var pantryIngIds = await _db.PantryItems
            .Where(p => p.WorkspaceId == wsId)
            .Select(p => p.IngredientId)
            .ToHashSetAsync(ct);

        var existingShoppingByIngId = await _db.ShoppingItems
            .Where(s => s.WorkspaceId == wsId)
            .ToDictionaryAsync(s => s.IngredientId, ct);

        // Collect unique ingredient names with their source meal
        var ingredientsToSync = plan.Days
            .Where(d => d.Meal is not null)
            .SelectMany(d => d.Meal!.Ingredients.Select(i => new { Name = i.Name.Trim(), MealName = d.Meal!.Name }))
            .DistinctBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Phase 1: Resolve IngredientMaster IDs and build the "needed" set
        var neededIngredientIds = new HashSet<Guid>();
        var ingredientsWithMaster = new List<(string MealName, IngredientMaster Master)>();

        foreach (var item in ingredientsToSync)
        {
            var master = await _db.IngredientMaster
                .FirstOrDefaultAsync(i =>
                    (i.WorkspaceId == null || i.WorkspaceId == wsId) &&
                    i.Name.ToLower() == item.Name.ToLower(), ct);

            if (master is null)
            {
                master = new IngredientMaster { Id = Guid.NewGuid(), Name = item.Name, WorkspaceId = wsId };
                _db.IngredientMaster.Add(master);
                await _db.SaveChangesAsync(ct);
            }

            if (!pantryIngIds.Contains(master.Id))
            {
                neededIngredientIds.Add(master.Id);
                ingredientsWithMaster.Add((item.MealName, master));
            }
        }

        // Phase 2: Remove stale plan-derived items (have Notes, no longer needed by current plan)
        foreach (var stale in existingShoppingByIngId.Values
            .Where(s => s.Notes != null && !neededIngredientIds.Contains(s.IngredientId))
            .ToList())
        {
            _db.ShoppingItems.Remove(stale);
            existingShoppingByIngId.Remove(stale.IngredientId);
        }

        // Phase 3: Add or update shopping items for needed ingredients
        foreach (var (mealName, master) in ingredientsWithMaster)
        {
            var note = $"{mealName} {weekLabel}";

            if (existingShoppingByIngId.TryGetValue(master.Id, out var existingShop))
            {
                if (existingShop.Notes is null || !existingShop.Notes.Contains(mealName))
                    existingShop.Notes = existingShop.Notes is null ? note : $"{existingShop.Notes}, {mealName} {weekLabel}";
                if (existingShop.IsChecked)
                {
                    existingShop.IsChecked = false;
                    existingShop.AddedAt = DateTime.UtcNow;
                }
            }
            else
            {
                var newItem = new ShoppingItem
                {
                    Id = Guid.NewGuid(),
                    WorkspaceId = wsId,
                    IngredientId = master.Id,
                    IsChecked = false,
                    AddedAt = DateTime.UtcNow,
                    Notes = note
                };
                _db.ShoppingItems.Add(newItem);
                existingShoppingByIngId[master.Id] = newItem;
            }
        }

        await _db.SaveChangesAsync(ct);
        return await GetAllAsync(ct);
    }

    /// <summary>
    /// Syncs the shopping list with the current ISO week's plan.
    /// If no plan exists for the current week, returns the list unchanged.
    /// </summary>
    public async Task<Result<List<ShoppingItemResponse>>> SyncFromCurrentWeeklyPlanAsync(CancellationToken ct)
    {
        var wsId = _ws.RequireCurrent();
        var today = DateTime.UtcNow.Date;
        var week = ISOWeek.GetWeekOfYear(today);
        var year = ISOWeek.GetYear(today);

        var planId = await _db.WeeklyPlans
            .AsNoTracking()
            .Where(p => p.WorkspaceId == wsId && p.Year == year && p.WeekNumber == week)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(ct);

        if (planId is null)
            return Result<List<ShoppingItemResponse>>.Success(await GetAllAsync(ct));

        return await SyncFromWeeklyPlanAsync(planId.Value, ct);
    }
}
