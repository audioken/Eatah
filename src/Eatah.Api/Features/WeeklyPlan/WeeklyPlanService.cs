using System.Globalization;
using Eatah.Api.Features.DietRules;
using Eatah.Api.Features.Meals;
using Eatah.Domain.Entities;

namespace Eatah.Api.Features.WeeklyPlan;

public class WeeklyPlanNotFoundException : Exception
{
    public WeeklyPlanNotFoundException(Guid id)
        : base($"Veckoplan med id {id} hittades inte.")
    {
    }
}

public class WeeklyPlanConflictException : Exception
{
    public WeeklyPlanConflictException(int year, int week)
        : base($"En veckoplan för {year} vecka {week} finns redan.")
    {
    }
}

public class DayPlanNotFoundException : Exception
{
    public DayPlanNotFoundException(Guid planId, DayOfWeek day)
        : base($"Dagen {day} finns inte i veckoplan {planId}.")
    {
    }
}

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

    public async Task<WeeklyPlanResponse> CreateAsync(CreateWeeklyPlanRequest request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByYearWeekAsync(request.Year, request.WeekNumber, cancellationToken);
        if (existing is not null)
        {
            throw new WeeklyPlanConflictException(request.Year, request.WeekNumber);
        }

        var plan = BuildEmptyPlan(request.Year, request.WeekNumber);
        await _repository.AddAsync(plan, cancellationToken);
        return ToResponse(plan);
    }

    public async Task<WeeklyPlanResponse> AssignMealAsync(
        Guid planId,
        DayOfWeek dayOfWeek,
        Guid mealId,
        CancellationToken cancellationToken)
    {
        var plan = await _repository.GetByIdAsync(planId, cancellationToken)
            ?? throw new WeeklyPlanNotFoundException(planId);

        var day = plan.Days.FirstOrDefault(d => d.DayOfWeek == dayOfWeek)
            ?? throw new DayPlanNotFoundException(planId, dayOfWeek);

        var meal = await _mealRepository.GetByIdAsync(mealId, cancellationToken)
            ?? throw new MealNotFoundException(mealId);

        day.MealId = meal.Id;
        day.Meal = meal;

        await _repository.SaveChangesAsync(cancellationToken);

        // Reload with tracked meal to ensure response includes category/name.
        var refreshed = await _repository.GetByIdAsync(planId, cancellationToken);
        return ToResponse(refreshed!);
    }

    public async Task<WeeklyPlanResponse> ClearDayAsync(
        Guid planId,
        DayOfWeek dayOfWeek,
        CancellationToken cancellationToken)
    {
        var plan = await _repository.GetByIdAsync(planId, cancellationToken)
            ?? throw new WeeklyPlanNotFoundException(planId);

        var day = plan.Days.FirstOrDefault(d => d.DayOfWeek == dayOfWeek)
            ?? throw new DayPlanNotFoundException(planId, dayOfWeek);

        day.MealId = null;
        day.Meal = null;

        await _repository.SaveChangesAsync(cancellationToken);
        return ToResponse(plan);
    }

    public async Task<WeeklyPlanResponse> RandomizeAsync(
        Guid planId,
        RandomizeWeeklyPlanRequest request,
        CancellationToken cancellationToken)
    {
        var plan = await _repository.GetByIdAsync(planId, cancellationToken)
            ?? throw new WeeklyPlanNotFoundException(planId);

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

    public async Task<WeeklyPlanResponse> RandomizeDayAsync(
        Guid planId,
        DayOfWeek dayOfWeek,
        RandomizeDayRequest request,
        CancellationToken cancellationToken)
    {
        var plan = await _repository.GetByIdAsync(planId, cancellationToken)
            ?? throw new WeeklyPlanNotFoundException(planId);

        var day = plan.Days.FirstOrDefault(d => d.DayOfWeek == dayOfWeek)
            ?? throw new DayPlanNotFoundException(planId, dayOfWeek);

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
