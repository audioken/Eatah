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
    private readonly IRealtimeNotifier _realtime;

    public IngredientCatalogService(EatahDbContext db, IWorkspaceContext ws, IRealtimeNotifier realtime)
    {
        _db = db;
        _ws = ws;
        _realtime = realtime;
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

    public async Task<Result<IngredientResponse>> UpdateAsync(Guid id, string name, string? category, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.BadRequest(ErrorCodes.IngredientNameRequired, "Ingredient name is required.");
        var wsId = _ws.RequireCurrent();
        var entity = await _db.IngredientMaster.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (entity is null)
            return Error.NotFound(ErrorCodes.IngredientNotFound, "Ingredient not found.");
        if (entity.WorkspaceId is null)
            return Error.Forbidden(ErrorCodes.IngredientSystemProtected, "System ingredients cannot be edited.");
        if (entity.WorkspaceId != wsId)
            return Error.Forbidden(ErrorCodes.IngredientSystemProtected, "Ingredient belongs to a different workspace.");
        var trimmed = name.Trim();
        var duplicate = await _db.IngredientMaster
            .AnyAsync(i => i.WorkspaceId == wsId && i.Id != id && i.Name == trimmed, ct);
        if (duplicate)
            return Error.Conflict(ErrorCodes.IngredientNameRequired, "An ingredient with that name already exists.");
        entity.Name = trimmed;
        entity.Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
        await _db.SaveChangesAsync(ct);
        return new IngredientResponse(entity.Id, entity.Name, entity.Category, false);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var wsId = _ws.RequireCurrent();
        var entity = await _db.IngredientMaster.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (entity is null)
            return Error.NotFound(ErrorCodes.IngredientNotFound, "Ingredient not found.");
        if (entity.WorkspaceId is null)
            return Error.Forbidden(ErrorCodes.IngredientSystemProtected, "System ingredients cannot be deleted.");
        if (entity.WorkspaceId != wsId)
            return Error.Forbidden(ErrorCodes.IngredientSystemProtected, "Ingredient belongs to a different workspace.");

        // Remove pantry items first (DB cascades to coverage rows via ON DELETE CASCADE).
        await _db.PantryItems.Where(p => p.IngredientId == id && p.WorkspaceId == wsId).ExecuteDeleteAsync(ct);
        // Remove shopping items.
        await _db.ShoppingItems.Where(s => s.IngredientId == id && s.WorkspaceId == wsId).ExecuteDeleteAsync(ct);

        _db.IngredientMaster.Remove(entity);
        await _db.SaveChangesAsync(ct);

        await _realtime.PantryChangedAsync(wsId, ct);
        await _realtime.ShoppingListChangedAsync(wsId, ct);
        return Result.Success();
    }
}

public class PantryService
{
    private readonly EatahDbContext _db;
    private readonly IWorkspaceContext _ws;
    private readonly IRealtimeNotifier _realtime;

    public PantryService(EatahDbContext db, IWorkspaceContext ws, IRealtimeNotifier realtime)
    {
        _db = db;
        _ws = ws;
        _realtime = realtime;
    }

    public async Task<List<PantryItemResponse>> GetAllAsync(CancellationToken ct)
    {
        var wsId = _ws.RequireCurrent();
        return await _db.PantryItems.AsNoTracking()
            .Include(p => p.Ingredient)
            .Where(p => p.WorkspaceId == wsId)
            .OrderByDescending(p => p.AddedAt)
            .Select(p => new PantryItemResponse(p.Id, p.IngredientId, p.Ingredient!.Name, p.Ingredient.Category, p.AddedAt, p.Ingredient.WorkspaceId == null))
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
        await _realtime.PantryChangedAsync(wsId, ct);
        return new PantryItemResponse(entity.Id, ing.Id, ing.Name, ing.Category, entity.AddedAt, ing.WorkspaceId == null);
    }

    public async Task<Result> RemoveAsync(Guid id, CancellationToken ct)
    {
        var wsId = _ws.RequireCurrent();
        var entity = await _db.PantryItems.FirstOrDefaultAsync(p => p.Id == id && p.WorkspaceId == wsId, ct);
        if (entity is null) return Error.NotFound(ErrorCodes.PantryItemNotFound, "Pantry item not found.");
        _db.PantryItems.Remove(entity);
        await _db.SaveChangesAsync(ct);
        await _realtime.PantryChangedAsync(wsId, ct);
        return Result.Success();
    }

    /// <summary>
    /// Returns all coverage answers (covers/declined) per (IngredientId, DayPlanId) for the current workspace.
    /// Absence of a row means the question is still pending for that pair.
    /// </summary>
    public async Task<List<PantryCoverageResponse>> GetCoverageAsync(CancellationToken ct)
    {
        var wsId = _ws.RequireCurrent();
        return await _db.PantryItemMealCoverages.AsNoTracking()
            .Where(c => c.PantryItem!.WorkspaceId == wsId)
            .Select(c => new PantryCoverageResponse(c.PantryItem!.IngredientId, c.DayPlanId, c.Covers))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Upserts the coverage answer for a (pantry ingredient, DayPlan) pair in the current workspace.
    /// Requires that the ingredient already exists in pantry.
    /// </summary>
    public async Task<Result<PantryCoverageResponse>> SetCoverageAsync(Guid ingredientId, Guid dayPlanId, bool covers, CancellationToken ct)
    {
        var wsId = _ws.RequireCurrent();
        var pantry = await _db.PantryItems
            .FirstOrDefaultAsync(p => p.WorkspaceId == wsId && p.IngredientId == ingredientId, ct);
        if (pantry is null) return Error.NotFound(ErrorCodes.PantryItemNotFound, "Ingredient not in pantry.");

        var existing = await _db.PantryItemMealCoverages
            .FirstOrDefaultAsync(c => c.PantryItemId == pantry.Id && c.DayPlanId == dayPlanId, ct);

        if (existing is null)
        {
            existing = new PantryItemMealCoverage
            {
                Id = Guid.NewGuid(),
                PantryItemId = pantry.Id,
                DayPlanId = dayPlanId,
                Covers = covers,
                AnsweredAt = DateTime.UtcNow
            };
            _db.PantryItemMealCoverages.Add(existing);
        }
        else
        {
            existing.Covers = covers;
            existing.AnsweredAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
        await _realtime.PantryChangedAsync(wsId, ct);
        return new PantryCoverageResponse(ingredientId, dayPlanId, covers);
    }
}

public class ShoppingListService
{
    private readonly EatahDbContext _db;
    private readonly IWorkspaceContext _ws;
    private readonly IRealtimeNotifier _realtime;
    private readonly WorkspaceLockProvider _locks;

    public ShoppingListService(EatahDbContext db, IWorkspaceContext ws, IRealtimeNotifier realtime, WorkspaceLockProvider locks)
    {
        _db = db;
        _ws = ws;
        _realtime = realtime;
        _locks = locks;
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
            await _realtime.ShoppingListChangedAsync(wsId, ct);
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
        await _realtime.ShoppingListChangedAsync(wsId, ct);
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
        await _realtime.ShoppingListChangedAsync(wsId, ct);
        if (isChecked) await _realtime.PantryChangedAsync(wsId, ct);
        return Result.Success();
    }

    public async Task<Result> RemoveAsync(Guid id, CancellationToken ct)
    {
        var wsId = _ws.RequireCurrent();
        var entity = await _db.ShoppingItems.FirstOrDefaultAsync(s => s.Id == id && s.WorkspaceId == wsId, ct);
        if (entity is null) return Error.NotFound(ErrorCodes.ShoppingItemNotFound, "Shopping item not found.");
        _db.ShoppingItems.Remove(entity);
        await _db.SaveChangesAsync(ct);
        await _realtime.ShoppingListChangedAsync(wsId, ct);
        return Result.Success();
    }

    public async Task ClearCheckedAsync(CancellationToken ct)
    {
        var wsId = _ws.RequireCurrent();
        await _db.ShoppingItems
            .Where(s => s.WorkspaceId == wsId && s.IsChecked)
            .ExecuteDeleteAsync(ct);
        await _realtime.ShoppingListChangedAsync(wsId, ct);
    }

    /// <summary>
    /// Syncs the shopping list with the given weekly plan: adds ingredients for currently
    /// planned meals (skipping pantry items where coverage has been confirmed for that
    /// specific DayPlan) and removes any previously-synced items that are no longer
    /// needed. Manually-added items (Notes == null) are never removed. Returns the full
    /// updated list.
    /// </summary>
    /// <remarks>
    /// Each scheduled DayPlan is an independent cooking session, so the same meal on
    /// multiple days (or across weeks) is tracked as separate entries and requires its
    /// own coverage answer — never collapse them by MealId.
    /// </remarks>
    public async Task<Result<List<ShoppingItemResponse>>> SyncFromWeeklyPlanAsync(Guid planId, CancellationToken ct, DayOfWeek? fromDayInclusive = null)
    {
        var wsId = _ws.RequireCurrent();

        // Serialize concurrent syncs for the same workspace — otherwise two clients can race
        // and produce duplicate/inconsistent shopping rows. xmin still protects across instances.
        using var _lock = await _locks.AcquireAsync(WorkspaceLockProvider.ScopeShoppingSync, wsId, ct);

        var plan = await _db.WeeklyPlans
            .AsNoTracking()
            .Include(p => p.Days)
                .ThenInclude(d => d.Meal)
                    .ThenInclude(m => m!.Ingredients)
            .FirstOrDefaultAsync(p => p.Id == planId && p.WorkspaceId == wsId, ct);

        if (plan is null)
            return Error.NotFound(ErrorCodes.WeeklyPlanNotFound, "Weekly plan not found.");

        var weekLabel = $"v{plan.WeekNumber}";
        // Match " {weekLabel}d" rather than " {weekLabel}" so e.g. "v2" doesn't collide with "v20"/"v21".
        var weekSentinel = $" {weekLabel}d";

        // Load existing pantry ingredient IDs and all shopping items (tracked for mutations)
        var pantryItems = await _db.PantryItems
            .Where(p => p.WorkspaceId == wsId)
            .Select(p => new { p.Id, p.IngredientId })
            .ToListAsync(ct);
        var pantryIngIds = pantryItems.Select(p => p.IngredientId).ToHashSet();
        var pantryItemIds = pantryItems.Select(p => p.Id).ToList();

        // Coverage: per IngredientId, set of DayPlanIds the pantry stock has been
        // confirmed to cover. Each DayPlan is its own cooking session so the same meal
        // on multiple days requires independent confirmation.
        var coverageRows = await _db.PantryItemMealCoverages
            .Where(c => pantryItemIds.Contains(c.PantryItemId) && c.Covers)
            .Join(_db.PantryItems, c => c.PantryItemId, p => p.Id, (c, p) => new { p.IngredientId, c.DayPlanId })
            .ToListAsync(ct);
        var coveredByIngredient = coverageRows
            .GroupBy(x => x.IngredientId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.DayPlanId).ToHashSet());

        var existingShoppingByIngId = await _db.ShoppingItems
            .Where(s => s.WorkspaceId == wsId)
            .ToDictionaryAsync(s => s.IngredientId, ct);

        static int DayIndex(DayOfWeek d) => d switch
        {
            DayOfWeek.Monday => 0,
            DayOfWeek.Tuesday => 1,
            DayOfWeek.Wednesday => 2,
            DayOfWeek.Thursday => 3,
            DayOfWeek.Friday => 4,
            DayOfWeek.Saturday => 5,
            _ => 6
        };

        // Group by ingredient name and keep one entry per DayPlan (never collapse by MealId).
        // The DayPlanId is the cooking-session identifier the rest of the pipeline uses for
        // coverage decisions and notes.
        var ingredientsToSync = plan.Days
            .Where(d => d.Meal is not null
                && (fromDayInclusive is null || DayIndex(d.DayOfWeek) >= DayIndex(fromDayInclusive.Value)))
            .SelectMany(d => d.Meal!.Ingredients.Select(i => new
            {
                Name = i.Name.Trim(),
                DayPlanId = d.Id,
                MealName = d.Meal!.Name,
                Day = DayIndex(d.DayOfWeek)
            }))
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => new
            {
                Name = g.Key,
                // Dedupe by DayPlanId only — in case a meal lists the same ingredient twice.
                SessionEntries = g.GroupBy(x => x.DayPlanId)
                               .Select(sg => (DayPlanId: sg.Key, MealName: sg.First().MealName, Day: sg.First().Day))
                               .ToList()
            })
            .ToList();

        // Phase 1: Resolve IngredientMaster IDs and build the "needed" set
        var neededIngredientIds = new HashSet<Guid>();
        var ingredientsWithMaster = new List<(List<(Guid DayPlanId, string MealName, int Day)> SessionEntries, IngredientMaster Master)>();

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
                ingredientsWithMaster.Add((item.SessionEntries, master));
            }
            else
            {
                // In pantry: drop sessions confirmed covered; the rest still need buying.
                var coveredSessions = coveredByIngredient.TryGetValue(master.Id, out var cov) ? cov : new HashSet<Guid>();
                var uncovered = item.SessionEntries.Where(e => !coveredSessions.Contains(e.DayPlanId)).ToList();
                if (uncovered.Count > 0)
                {
                    neededIngredientIds.Add(master.Id);
                    ingredientsWithMaster.Add((uncovered, master));
                }
            }
        }

        // Phase 2: Remove stale week-N entries from items no longer needed at all.
        // Other-week entries on the same item are preserved (multi-week sync).
        foreach (var existing in existingShoppingByIngId.Values
            .Where(s => s.Notes is not null && s.Notes.Contains(weekSentinel) && !neededIngredientIds.Contains(s.IngredientId))
            .ToList())
        {
            var remainingEntries = existing.Notes!
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(part => !part.Contains(weekSentinel))
                .ToList();

            if (remainingEntries.Count == 0)
            {
                _db.ShoppingItems.Remove(existing);
                existingShoppingByIngId.Remove(existing.IngredientId);
            }
            else
            {
                existing.Notes = string.Join(", ", remainingEntries);
            }
        }

        // Phase 3: Add or update shopping items for needed ingredients.
        // Notes are merged per week: other-week entries are preserved, this week's entries are replaced.
        // Note format: "MealName v22d4|<DayPlanId:N>" — DayPlanId embedded so the client can
        // persist per-session coverage decisions back to PantryItemMealCoverage.
        foreach (var (sessionEntries, master) in ingredientsWithMaster)
        {
            var newNoteEntries = sessionEntries.Select(e => $"{e.MealName} {weekLabel}d{e.Day}|{e.DayPlanId:N}");

            if (existingShoppingByIngId.TryGetValue(master.Id, out var existingShop))
            {
                var otherWeekEntries = existingShop.Notes is null ? [] :
                    existingShop.Notes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Where(part => !part.Contains(weekSentinel));
                existingShop.Notes = string.Join(", ", otherWeekEntries.Concat(newNoteEntries));
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
                    Notes = string.Join(", ", newNoteEntries)
                };
                _db.ShoppingItems.Add(newItem);
                existingShoppingByIngId[master.Id] = newItem;
            }
        }

        await _db.SaveChangesAsync(ct);
        await _realtime.ShoppingListChangedAsync(wsId, ct);
        return await GetAllAsync(ct);
    }

    /// <summary>
    /// Syncs the shopping list with the current AND next ISO week's plan.
    /// Items from both weeks are merged in a single list (max 2 weeks visible).
    /// If no plan exists for a week, that week's entries are left unchanged.
    /// </summary>
    public async Task<Result<List<ShoppingItemResponse>>> SyncFromCurrentWeeklyPlanAsync(CancellationToken ct)
    {
        var wsId = _ws.RequireCurrent();
        var today = DateTime.UtcNow.Date;
        var currentWeek = ISOWeek.GetWeekOfYear(today);
        var currentYear = ISOWeek.GetYear(today);

        var nextWeekDate = today.AddDays(7);
        var nextWeek = ISOWeek.GetWeekOfYear(nextWeekDate);
        var nextYear = ISOWeek.GetYear(nextWeekDate);

        // Sync current week (from today onward)
        var currentPlanId = await _db.WeeklyPlans
            .AsNoTracking()
            .Where(p => p.WorkspaceId == wsId && p.Year == currentYear && p.WeekNumber == currentWeek)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(ct);

        if (currentPlanId.HasValue)
        {
            var result = await SyncFromWeeklyPlanAsync(currentPlanId.Value, ct, fromDayInclusive: today.DayOfWeek);
            if (!result.IsSuccess) return result;
        }

        // Sync next week (all days)
        var nextPlanId = await _db.WeeklyPlans
            .AsNoTracking()
            .Where(p => p.WorkspaceId == wsId && p.Year == nextYear && p.WeekNumber == nextWeek)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(ct);

        if (nextPlanId.HasValue)
        {
            var result = await SyncFromWeeklyPlanAsync(nextPlanId.Value, ct);
            if (!result.IsSuccess) return result;
        }

        return await GetAllAsync(ct);
    }
}
