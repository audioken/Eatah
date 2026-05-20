using FluentValidation;

namespace Eatah.Api.Features.WeeklyPlan;

public static class WeeklyPlanEndpoints
{
    public static IEndpointRouteBuilder MapWeeklyPlanEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/weeklyplans")
            .WithTags("WeeklyPlans")
            .RequireAuthorization();

        group.MapGet("/current", GetCurrentWeeklyPlan.Handle).WithName(nameof(GetCurrentWeeklyPlan));
        group.MapGet("/by-week", GetWeeklyPlanByWeek.Handle).WithName(nameof(GetWeeklyPlanByWeek));
        group.MapPost("/", CreateWeeklyPlan.Handle).WithName(nameof(CreateWeeklyPlan));
        group.MapPut("/{id:guid}/days/{dayOfWeek}", AssignMealToDay.Handle).WithName(nameof(AssignMealToDay));
        group.MapDelete("/{id:guid}/days/{dayOfWeek}", ClearMealFromDay.Handle).WithName(nameof(ClearMealFromDay));
        group.MapPost("/{id:guid}/randomize", RandomizeWeeklyPlan.Handle).WithName(nameof(RandomizeWeeklyPlan));
        group.MapPost("/{id:guid}/days/{dayOfWeek}/randomize", RandomizeDay.Handle).WithName(nameof(RandomizeDay));
        group.MapPut("/{id:guid}/diet-profile", UpdateWeeklyPlanDietProfile.Handle).WithName(nameof(UpdateWeeklyPlanDietProfile));

        return app;
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
        services.AddScoped<IValidator<RandomizeDayRequest>, RandomizeDayRequestValidator>();
        return services;
    }
}
