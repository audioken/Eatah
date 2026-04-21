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

    public WeeklyPlanService(
        IWeeklyPlanRepository repository,
        IMealRepository mealRepository,
        IDietProfileRepository profileRepository,
        IRandomMealGenerator generator)
    {
        _repository = repository;
        _mealRepository = mealRepository;
        _profileRepository = profileRepository;
        _generator = generator;
    }

    public async Task<WeeklyPlanResponse> GetCurrentAsync(CancellationToken cancellationToken)
    {
        var (year, week) = GetCurrentIsoWeek();
        var plan = await _repository.GetByYearWeekAsync(year, week, cancellationToken);
        if (plan is null)
        {
            plan = BuildEmptyPlan(year, week);
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

        DietProfile? profile = null;
        if (request.ProfileId is Guid profileId)
        {
            profile = await _profileRepository.GetByIdAsync(profileId, cancellationToken);
        }

        var orderedDays = plan.Days.OrderBy(d => DayOrderIndex(d.DayOfWeek)).ToList();
        var dayOrder = orderedDays.Select(d => d.DayOfWeek).ToList();

        var assignments = _generator.Generate(meals, dayOrder, profile, request.Strictness);

        for (var i = 0; i < orderedDays.Count; i++)
        {
            var chosen = assignments[i];
            orderedDays[i].MealId = chosen?.Id;
            orderedDays[i].Meal = chosen;
        }

        await _repository.SaveChangesAsync(cancellationToken);

        var refreshed = await _repository.GetByIdAsync(planId, cancellationToken);
        return ToResponse(refreshed!);
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

        DietProfile? profile = null;
        if (request.ProfileId is Guid profileId)
        {
            profile = await _profileRepository.GetByIdAsync(profileId, cancellationToken);
        }

        var chosen = _generator.GenerateForDay(meals, plan, dayOfWeek, profile, request.Strictness);

        day.MealId = chosen?.Id;
        day.Meal = chosen;

        await _repository.SaveChangesAsync(cancellationToken);

        var refreshed = await _repository.GetByIdAsync(planId, cancellationToken);
        return ToResponse(refreshed!);
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
}

internal static class WeeklyPlanErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound(ErrorCodes.WeeklyPlanNotFound, $"Weekly plan with id {id} was not found.");

    public static Error Conflict(int year, int week) =>
        Error.Conflict(ErrorCodes.WeeklyPlanConflict, $"A weekly plan for {year} week {week} already exists.");

    public static Error DayNotFound(Guid planId, DayOfWeek day) =>
        Error.NotFound(ErrorCodes.DayPlanNotFound, $"Day {day} was not found in weekly plan {planId}.");
}
