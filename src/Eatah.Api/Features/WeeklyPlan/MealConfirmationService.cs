using System.Globalization;
using Eatah.Api.Common;
using Eatah.Domain.Entities;
using Eatah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Eatah.Api.Features.WeeklyPlan;

public class MealConfirmationService
{
    private readonly EatahDbContext _db;
    private readonly IWorkspaceContext _ws;
    private readonly IRealtimeNotifier _realtime;

    public MealConfirmationService(EatahDbContext db, IWorkspaceContext ws, IRealtimeNotifier realtime)
    {
        _db = db;
        _ws = ws;
        _realtime = realtime;
    }

    /// <summary>
    /// Returns all past DayPlans with assigned meals that have not yet been confirmed or skipped.
    /// "Past" means the ISO date of the day is strictly before today (UTC).
    /// </summary>
    public async Task<List<PendingConfirmationResponse>> GetPendingAsync(CancellationToken ct)
    {
        var wsId = _ws.RequireCurrent();
        var today = DateTime.UtcNow.Date;

        var plans = await _db.WeeklyPlans
            .AsNoTracking()
            .Include(wp => wp.Days)
                .ThenInclude(d => d.Meal)
            .Where(wp => wp.WorkspaceId == wsId)
            .ToListAsync(ct);

        var results = new List<PendingConfirmationResponse>();
        foreach (var wp in plans)
        {
            foreach (var dp in wp.Days.Where(d => d.Meal is not null && d.ConfirmationStatus is null))
            {
                var dayDate = ISOWeek.ToDateTime(wp.Year, wp.WeekNumber, dp.DayOfWeek);
                if (dayDate.Date < today)
                    results.Add(new PendingConfirmationResponse(dp.Id, dp.Meal!.Name, dp.DayOfWeek, wp.WeekNumber, wp.Year));
            }
        }

        return results
            .OrderBy(r => r.Year)
            .ThenBy(r => r.WeekNumber)
            .ThenBy(r => DayIndex(r.DayOfWeek))
            .ToList();
    }

    /// <summary>
    /// Confirms or skips a batch of past meals.
    /// Eaten = true  → deletes pantry coverage rows for that DayPlan; removes pantry items that have
    ///                  no remaining coverage; strips the DayPlanId note entry from shopping items.
    /// Eaten = false → only strips the DayPlanId note entry from shopping items (stock is untouched).
    /// Already-confirmed DayPlans are silently skipped.
    /// </summary>
    public async Task<Result> ConfirmAsync(List<ConfirmMealItem> confirmations, CancellationToken ct)
    {
        if (confirmations.Count == 0) return Result.Success();

        var wsId = _ws.RequireCurrent();

        // Load all weekly plans for this workspace to verify ownership
        var plans = await _db.WeeklyPlans
            .Include(wp => wp.Days)
            .Where(wp => wp.WorkspaceId == wsId)
            .ToListAsync(ct);

        var allDayPlanIds = confirmations.Select(c => c.DayPlanId).ToHashSet();
        var dayPlansById = plans
            .SelectMany(p => p.Days)
            .Where(d => allDayPlanIds.Contains(d.Id))
            .ToDictionary(d => d.Id);

        if (dayPlansById.Count != allDayPlanIds.Count)
            return Error.Forbidden(ErrorCodes.WorkspaceAccessDenied, "One or more day plans do not belong to this workspace.");

        var eatenDayPlanIds = confirmations
            .Where(c => c.Eaten)
            .Select(c => c.DayPlanId)
            .ToList();

        bool pantryChanged = false;
        bool shoppingChanged = false;

        // ── Phase 1: Handle Eaten meals ──────────────────────────────────────
        if (eatenDayPlanIds.Count > 0)
        {
            // Load all coverage rows for eaten DayPlans in one query
            var coverageToDelete = await _db.PantryItemMealCoverages
                .Where(c => eatenDayPlanIds.Contains(c.DayPlanId) && c.Covers)
                .ToListAsync(ct);

            if (coverageToDelete.Count > 0)
            {
                var affectedPantryItemIds = coverageToDelete.Select(c => c.PantryItemId).Distinct().ToList();

                // Count total coverage per pantry item in DB (before delete)
                var totalCoveragePerItem = await _db.PantryItemMealCoverages
                    .Where(c => affectedPantryItemIds.Contains(c.PantryItemId))
                    .GroupBy(c => c.PantryItemId)
                    .Select(g => new { PantryItemId = g.Key, Count = g.Count() })
                    .ToListAsync(ct);

                var deletedCountPerItem = coverageToDelete
                    .GroupBy(c => c.PantryItemId)
                    .ToDictionary(g => g.Key, g => g.Count());

                _db.PantryItemMealCoverages.RemoveRange(coverageToDelete);
                pantryChanged = true;

                // Remove pantry items whose coverage is fully consumed
                foreach (var item in totalCoveragePerItem)
                {
                    deletedCountPerItem.TryGetValue(item.PantryItemId, out var beingDeleted);
                    if (item.Count == beingDeleted)
                    {
                        var pantryItem = await _db.PantryItems
                            .FirstOrDefaultAsync(p => p.Id == item.PantryItemId, ct);
                        if (pantryItem is not null)
                            _db.PantryItems.Remove(pantryItem);
                    }
                }
            }
        }

        // ── Phase 2: Strip DayPlanId note entries from shopping list ────────
        // Applies to both Eaten and Skipped (neither needs buying anymore)
        var allDayPlanIdStrs = allDayPlanIds.Select(id => $"|{id:N}").ToList();

        var shoppingItemsToUpdate = await _db.ShoppingItems
            .Where(s => s.WorkspaceId == wsId && s.Notes != null)
            .ToListAsync(ct);

        foreach (var si in shoppingItemsToUpdate)
        {
            var parts = si.Notes!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var retained = parts.Where(p => !allDayPlanIdStrs.Any(p.Contains)).ToList();

            if (retained.Count == parts.Length) continue;

            shoppingChanged = true;
            if (retained.Count == 0)
                _db.ShoppingItems.Remove(si);
            else
                si.Notes = string.Join(", ", retained);
        }

        // ── Phase 3: Mark DayPlan status ──────────────────────────────────
        var now = DateTime.UtcNow;
        foreach (var item in confirmations)
        {
            if (!dayPlansById.TryGetValue(item.DayPlanId, out var dp)) continue;
            if (dp.ConfirmationStatus is not null) continue; // already confirmed

            dp.ConfirmationStatus = item.Eaten ? MealConfirmationStatus.Eaten : MealConfirmationStatus.Skipped;
            dp.ConfirmedAt = now;
        }

        await _db.SaveChangesAsync(ct);

        if (pantryChanged) await _realtime.PantryChangedAsync(wsId, ct);
        if (shoppingChanged) await _realtime.ShoppingListChangedAsync(wsId, ct);

        return Result.Success();
    }

    private static int DayIndex(DayOfWeek d) => d switch
    {
        DayOfWeek.Monday => 0,
        DayOfWeek.Tuesday => 1,
        DayOfWeek.Wednesday => 2,
        DayOfWeek.Thursday => 3,
        DayOfWeek.Friday => 4,
        DayOfWeek.Saturday => 5,
        _ => 6
    };
}
