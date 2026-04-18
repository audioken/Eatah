using Eatah.Api.Features.Meals;
using FluentValidation;

namespace Eatah.Api.Features.WeeklyPlan;

public static class WeeklyPlanEndpoints
{
    public static IEndpointRouteBuilder MapWeeklyPlanEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/weeklyplans")
            .WithTags("WeeklyPlans");

        group.MapGet("/current", GetCurrent)
            .WithName("GetCurrentWeeklyPlan");

        group.MapPost("/", Create)
            .WithName("CreateWeeklyPlan");

        group.MapPut("/{id:guid}/days/{dayOfWeek}", AssignMeal)
            .WithName("AssignMealToDay");

        group.MapDelete("/{id:guid}/days/{dayOfWeek}", ClearDay)
            .WithName("ClearMealFromDay");

        group.MapPost("/{id:guid}/randomize", Randomize)
            .WithName("RandomizeWeeklyPlan");

        return app;
    }

    private static async Task<IResult> GetCurrent(WeeklyPlanService service, CancellationToken cancellationToken)
    {
        var plan = await service.GetCurrentAsync(cancellationToken);
        return Results.Ok(plan);
    }

    private static async Task<IResult> Create(
        CreateWeeklyPlanRequest request,
        WeeklyPlanService service,
        IValidator<CreateWeeklyPlanRequest> validator,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        try
        {
            var plan = await service.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/weeklyplans/{plan.Id}", plan);
        }
        catch (WeeklyPlanConflictException ex)
        {
            return Results.Conflict(new { detail = ex.Message });
        }
    }

    private static async Task<IResult> AssignMeal(
        Guid id,
        DayOfWeek dayOfWeek,
        AssignMealRequest request,
        WeeklyPlanService service,
        IValidator<AssignMealRequest> validator,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        try
        {
            var plan = await service.AssignMealAsync(id, dayOfWeek, request.MealId, cancellationToken);
            return Results.Ok(plan);
        }
        catch (WeeklyPlanNotFoundException ex)
        {
            return Results.NotFound(new { detail = ex.Message });
        }
        catch (DayPlanNotFoundException ex)
        {
            return Results.NotFound(new { detail = ex.Message });
        }
        catch (MealNotFoundException ex)
        {
            return Results.NotFound(new { detail = ex.Message });
        }
    }

    private static async Task<IResult> ClearDay(
        Guid id,
        DayOfWeek dayOfWeek,
        WeeklyPlanService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var plan = await service.ClearDayAsync(id, dayOfWeek, cancellationToken);
            return Results.Ok(plan);
        }
        catch (WeeklyPlanNotFoundException ex)
        {
            return Results.NotFound(new { detail = ex.Message });
        }
        catch (DayPlanNotFoundException ex)
        {
            return Results.NotFound(new { detail = ex.Message });
        }
    }

    private static async Task<IResult> Randomize(
        Guid id,
        RandomizeWeeklyPlanRequest request,
        WeeklyPlanService service,
        IValidator<RandomizeWeeklyPlanRequest> validator,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        try
        {
            var plan = await service.RandomizeAsync(id, request, cancellationToken);
            return Results.Ok(plan);
        }
        catch (WeeklyPlanNotFoundException ex)
        {
            return Results.NotFound(new { detail = ex.Message });
        }
    }
}

public static class WeeklyPlanServiceExtensions
{
    public static IServiceCollection AddWeeklyPlanFeature(this IServiceCollection services)
    {
        services.AddScoped<IWeeklyPlanRepository, WeeklyPlanRepository>();
        services.AddScoped<WeeklyPlanService>();
        services.AddScoped<IRandomMealGenerator, RandomMealGenerator>();
        services.AddScoped<IValidator<CreateWeeklyPlanRequest>, CreateWeeklyPlanRequestValidator>();
        services.AddScoped<IValidator<AssignMealRequest>, AssignMealRequestValidator>();
        services.AddScoped<IValidator<RandomizeWeeklyPlanRequest>, RandomizeWeeklyPlanRequestValidator>();
        return services;
    }
}
