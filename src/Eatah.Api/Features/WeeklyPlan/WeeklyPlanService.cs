using System.Globalization;
using Eatah.Api.Common;
using Eatah.Api.Features.DietRules;
using Eatah.Api.Features.Meals;
using Eatah.Domain.Entities;

namespace Eatah.Api.Features.WeeklyPlan;

public class WeeklyPlanService
{
    private static readonly DayOfWeek[] WeekDaysInOrder =
    [
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday,
        DayOfWeek.Sunday
    ];

    private readonly IWeeklyPlanRepository _repository;
    private readonly IMealRepository _mealRepository;
    private readonly IDietProfileRepository _profileRepository;
    private readonly IRandomMealGenerator _generator;
    private readonly ILogger<WeeklyPlanService> _logger;

    public WeeklyPlanService(
        IWeeklyPlanRepository repository,
        IMealRepository mealRepository,
        IDietProfileRepository profileRepository,
        IRandomMealGenerator generator,
        ILogger<WeeklyPlanService> logger)
    {
        _repository = repository;
        _mealRepository = mealRepository;
        _profileRepository = profileRepository;
        _generator = generator;
        _logger = logger;
    }

    public async Task<WeeklyPlanResponse> GetCurrentAsync(CancellationToken cancellationToken)
    {
        var (year, week) = GetCurrentIsoWeek();
        return await GetOrCreateByWeekAsync(year, week, cancellationToken);
    }

    public async Task<WeeklyPlanResponse> GetOrCreateByWeekAsync(int year, int weekNumber, CancellationToken cancellationToken)
    {
        var plan = await _repository.GetByYearWeekAsync(year, weekNumber, cancellationToken);
        if (plan is null)
        {
            plan = BuildEmptyPlan(year, weekNumber);
            await _repository.AddAsync(plan, cancellationToken);
        }

        return ToResponse(plan);
    }

    public async Task<Result<WeeklyPlanResponse>> CreateAsync(CreateWeeklyPlanRequest request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByYearWeekAsync(request.Year, request.WeekNumber, cancellationToken);
        if (existing is not null)
        {
            return WeeklyPlanErrors.Conflict(request.Year, request.WeekNumber);
        }

        var plan = BuildEmptyPlan(request.Year, request.WeekNumber);
        await _repository.AddAsync(plan, cancellationToken);
        return ToResponse(plan);
    }

    public async Task<Result<WeeklyPlanResponse>> AssignMealAsync(
        Guid planId,
        DayOfWeek dayOfWeek,
        Guid mealId,
        CancellationToken cancellationToken)
    {
        var plan = await _repository.GetByIdAsync(planId, cancellationToken);
        if (plan is null)
        {
            return WeeklyPlanErrors.NotFound(planId);
        }

        var day = plan.Days.FirstOrDefault(d => d.DayOfWeek == dayOfWeek);
        if (day is null)
        {
            return WeeklyPlanErrors.DayNotFound(planId, dayOfWeek);
        }

        var meal = await _mealRepository.GetByIdAsync(mealId, cancellationToken);
        if (meal is null)
        {
            return Error.NotFound(ErrorCodes.MealNotFound, $"Meal with id {mealId} was not found.");
        }

        // Reset coverage answers for this slot — the new meal's ingredients must be re-confirmed.
        await _repository.DeleteCoverageForDayPlansAsync([day.Id], cancellationToken);

        day.MealId = meal.Id;
        day.Meal = meal;

        await _repository.SaveChangesAsync(cancellationToken);

        // Reload with tracked meal to ensure response includes category/name.
        var refreshed = await _repository.GetByIdAsync(planId, cancellationToken);
        return ToResponse(refreshed!);
    }

    public async Task<Result<WeeklyPlanResponse>> ClearDayAsync(
        Guid planId,
        DayOfWeek dayOfWeek,
        CancellationToken cancellationToken)
    {
        var plan = await _repository.GetByIdAsync(planId, cancellationToken);
        if (plan is null)
        {
            return WeeklyPlanErrors.NotFound(planId);
        }

        var day = plan.Days.FirstOrDefault(d => d.DayOfWeek == dayOfWeek);
        if (day is null)
        {
            return WeeklyPlanErrors.DayNotFound(planId, dayOfWeek);
        }

        // Reset coverage answers for this slot when the meal is cleared.
        await _repository.DeleteCoverageForDayPlansAsync([day.Id], cancellationToken);

        day.MealId = null;
        day.Meal = null;

        await _repository.SaveChangesAsync(cancellationToken);
        return ToResponse(plan);
    }

    public async Task<Result<WeeklyPlanResponse>> RandomizeAsync(
        Guid planId,
        RandomizeWeeklyPlanRequest request,
        CancellationToken cancellationToken)
    {
        var plan = await _repository.GetByIdAsync(planId, cancellationToken);
        if (plan is null)
        {
            return WeeklyPlanErrors.NotFound(planId);
        }

        var meals = await _mealRepository.GetAllAsync(cancellationToken);
        if (meals.Count == 0)
        {
            return WeeklyPlanErrors.MealsInsufficient();
        }

        DietProfile? profile = null;
        if (request.ProfileId is Guid profileId)
        {
            profile = await _profileRepository.GetByIdAsync(profileId, cancellationToken);
        }

        // Split days into past (locked) and future (to randomize).
        var today = DateTime.UtcNow.Date;
        var weekMonday = ISOWeek.GetYearStart(plan.Year).AddDays(7 * (plan.WeekNumber - 1));

        var orderedDays = plan.Days.OrderBy(d => DayOrderIndex(d.DayOfWeek)).ToList();
        var pastDays = orderedDays.Where(d => weekMonday.AddDays(DayOrderIndex(d.DayOfWeek)) < today).ToList();
        var futureDays = orderedDays.Where(d => weekMonday.AddDays(DayOrderIndex(d.DayOfWeek)) >= today).ToList();

        if (futureDays.Count == 0)
        {
            // All days are locked – nothing to randomize.
            return BuildResponseFromMealLookup(plan, meals);
        }

        // Count meals already assigned to past days so the generator can respect
        // the diet profile budget for the whole week. Past meals whose category isn't
        // allowed by the current profile (excluded or unknown) are ignored — otherwise
        // switching profile mid-week would let the past eat into the new budget.
        var mealLookup = meals.ToDictionary(m => m.Id);
        var rulesByCategory = profile?.Rules.ToDictionary(r => r.Category);
        var preAssignedCounts = new Dictionary<MealCategory, int>();
        foreach (var pastDay in pastDays)
        {
            if (pastDay.MealId is not Guid pastMealId) continue;
            if (!mealLookup.TryGetValue(pastMealId, out var pastMeal)) continue;
            if (rulesByCategory is not null &&
                (!rulesByCategory.TryGetValue(pastMeal.Category, out var rule) || rule.MaxPerWeek <= 0))
                continue;
            preAssignedCounts[pastMeal.Category] = preAssignedCounts.GetValueOrDefault(pastMeal.Category, 0) + 1;
        }

        var futureDayOrder = futureDays.Select(d => d.DayOfWeek).ToList();
        var assignments = _generator.Generate(meals, futureDayOrder, profile, preAssignedCounts);

        // Reset coverage answers for all days being re-randomized.
        await _repository.DeleteCoverageForDayPlansAsync(futureDays.Select(d => d.Id), cancellationToken);

        for (var i = 0; i < futureDays.Count; i++)
        {
            var chosen = assignments[i];
            // Only update the FK. Setting `Meal = null` on a tracked entity triggers EF's
            // relationship fixup which would also revert `MealId` to null. The response is
            // built below from the detached `meals` lookup, so the stale navigation is unused.
            futureDays[i].MealId = chosen?.Id;
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return BuildResponseFromMealLookup(plan, meals);
    }

    public async Task<Result<WeeklyPlanResponse>> RandomizeDayAsync(
        Guid planId,
        DayOfWeek dayOfWeek,
        RandomizeDayRequest request,
        CancellationToken cancellationToken)
    {
        var plan = await _repository.GetByIdAsync(planId, cancellationToken);
        if (plan is null)
        {
            return WeeklyPlanErrors.NotFound(planId);
        }

        var day = plan.Days.FirstOrDefault(d => d.DayOfWeek == dayOfWeek);
        if (day is null)
        {
            return WeeklyPlanErrors.DayNotFound(planId, dayOfWeek);
        }

        var meals = await _mealRepository.GetAllAsync(cancellationToken);
        if (meals.Count == 0)
        {
            return WeeklyPlanErrors.MealsInsufficient();
        }

        DietProfile? profile = null;
        if (request.ProfileId is Guid profileId)
        {
            profile = await _profileRepository.GetByIdAsync(profileId, cancellationToken);
        }

        var chosen = _generator.GenerateForDay(meals, plan, dayOfWeek, profile);

        _logger.LogInformation(
            "RandomizeDay plan={PlanId} day={Day} profile={Profile} otherDays=[{Other}] -> chosen={Chosen} ({Category})",
            planId,
            dayOfWeek,
            profile is null ? "<none>" : $"{profile.Name} [{string.Join(",", profile.Rules.Select(r => $"{r.Category}:{r.MinPerWeek}-{r.MaxPerWeek}"))}]",
            string.Join(", ", plan.Days.Where(d => d.DayOfWeek != dayOfWeek && d.Meal is not null).Select(d => $"{d.DayOfWeek}:{d.Meal!.Category}")),
            chosen?.Name ?? "<null>",
            chosen?.Category.ToString() ?? "-");

        // Reset coverage answers for this slot — the new meal's ingredients must be re-confirmed.
        await _repository.DeleteCoverageForDayPlansAsync([day.Id], cancellationToken);

        // Only update the FK (see RandomizeAsync for rationale).
        day.MealId = chosen?.Id;

        await _repository.SaveChangesAsync(cancellationToken);

        return BuildResponseFromMealLookup(plan, meals);
    }

    private static Eatah.Domain.Entities.WeeklyPlan BuildEmptyPlan(int year, int week)
    {
        return new Eatah.Domain.Entities.WeeklyPlan
        {
            Id = Guid.NewGuid(),
            Year = year,
            WeekNumber = week,
            Days = WeekDaysInOrder
                .Select(day => new DayPlan
                {
                    Id = Guid.NewGuid(),
                    DayOfWeek = day,
                    MealId = null,
                    Meal = null
                })
                .ToList()
        };
    }

    private static (int Year, int Week) GetCurrentIsoWeek()
    {
        var today = DateTime.UtcNow.Date;
        var calendar = CultureInfo.InvariantCulture.Calendar;
        var week = ISOWeek.GetWeekOfYear(today);
        var year = ISOWeek.GetYear(today);
        return (year, week);
    }

    private static int DayOrderIndex(DayOfWeek day)
    {
        return Array.IndexOf(WeekDaysInOrder, day);
    }

    internal static WeeklyPlanResponse ToResponse(Eatah.Domain.Entities.WeeklyPlan plan)
    {
        var days = plan.Days
            .OrderBy(d => DayOrderIndex(d.DayOfWeek))
            .Select(d => new DayPlanResponse(
                d.Id,
                d.DayOfWeek,
                d.MealId,
                d.Meal is null
                    ? null
                    : new MealSummaryResponse(d.Meal.Id, d.Meal.Name, d.Meal.Category)))
            .ToList();

        return new WeeklyPlanResponse(plan.Id, plan.Year, plan.WeekNumber, plan.CreatedAt, days);
    }

    /// <summary>
    /// Builds the response without relying on the <see cref="DayPlan.Meal"/> navigation property.
    /// Used after randomization, where the navigation may be null on tracked entities even though
    /// <see cref="DayPlan.MealId"/> was just updated. Resolves meal summaries from a detached lookup.
    /// </summary>
    private static WeeklyPlanResponse BuildResponseFromMealLookup(
        Eatah.Domain.Entities.WeeklyPlan plan,
        IReadOnlyList<Meal> mealLookupSource)
    {
        var lookup = mealLookupSource.ToDictionary(m => m.Id);
        var days = plan.Days
            .OrderBy(d => DayOrderIndex(d.DayOfWeek))
            .Select(d =>
            {
                MealSummaryResponse? summary = null;
                if (d.MealId is Guid mealId && lookup.TryGetValue(mealId, out var meal))
                {
                    summary = new MealSummaryResponse(meal.Id, meal.Name, meal.Category);
                }
                return new DayPlanResponse(d.Id, d.DayOfWeek, d.MealId, summary);
            })
            .ToList();

        return new WeeklyPlanResponse(plan.Id, plan.Year, plan.WeekNumber, plan.CreatedAt, days);
    }
}

internal static class WeeklyPlanErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound(ErrorCodes.WeeklyPlanNotFound, $"Weekly plan with id {id} was not found.");

    public static Error Conflict(int year, int week) =>
        Error.Conflict(ErrorCodes.WeeklyPlanConflict, $"A weekly plan for {year} week {week} already exists.");

    public static Error DayNotFound(Guid planId, DayOfWeek day) =>
        Error.NotFound(ErrorCodes.DayPlanNotFound, $"Day {day} was not found in weekly plan {planId}.");

    public static Error MealsInsufficient() =>
        Error.BadRequest(ErrorCodes.MealsInsufficient, "Not enough meals available to randomize.");
}
