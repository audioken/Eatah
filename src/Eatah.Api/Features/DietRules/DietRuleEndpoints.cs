namespace Eatah.Api.Features.DietRules;

public static class DietRuleEndpoints
{
    public static IEndpointRouteBuilder MapDietRuleEndpoints(this IEndpointRouteBuilder app)
    {
        var profiles = app.MapGroup("/api/dietprofiles")
            .WithTags("DietProfiles");

        profiles.MapGet("/", GetAllDietProfiles.Handle).WithName(nameof(GetAllDietProfiles));
        profiles.MapGet("/{id:guid}", GetDietProfileById.Handle).WithName(nameof(GetDietProfileById));

        app.MapPost("/api/weeklyplans/{id:guid}/evaluate", EvaluateWeeklyPlan.Handle)
            .WithTags("WeeklyPlans")
            .WithName(nameof(EvaluateWeeklyPlan));

        return app;
    }
}

public static class DietRuleServiceExtensions
{
    public static IServiceCollection AddDietRuleFeature(this IServiceCollection services)
    {
        services.AddScoped<IDietProfileRepository, DietProfileRepository>();
        services.AddScoped<IDietRuleEvaluator, DietRuleEvaluator>();
        services.AddScoped<DietRuleService>();
        return services;
    }
}
