using Eatah.Api.Features.WeeklyPlan;

namespace Eatah.Api.Features.DietRules;

public static class DietRuleEndpoints
{
    public static IEndpointRouteBuilder MapDietRuleEndpoints(this IEndpointRouteBuilder app)
    {
        var profiles = app.MapGroup("/api/dietprofiles")
            .WithTags("DietProfiles");

        profiles.MapGet("/", GetAllProfiles).WithName("GetAllDietProfiles");
        profiles.MapGet("/{id:guid}", GetProfileById).WithName("GetDietProfileById");

        app.MapPost("/api/weeklyplans/{id:guid}/evaluate", EvaluatePlan)
            .WithTags("WeeklyPlans")
            .WithName("EvaluateWeeklyPlan");

        return app;
    }

    private static async Task<IResult> GetAllProfiles(DietRuleService service, CancellationToken cancellationToken)
    {
        var profiles = await service.GetAllProfilesAsync(cancellationToken);
        return Results.Ok(profiles);
    }

    private static async Task<IResult> GetProfileById(Guid id, DietRuleService service, CancellationToken cancellationToken)
    {
        var profile = await service.GetProfileAsync(id, cancellationToken);
        return profile is null
            ? Results.NotFound(new { detail = $"Kostprofil med id {id} hittades inte." })
            : Results.Ok(profile);
    }

    private static async Task<IResult> EvaluatePlan(
        Guid id,
        Guid profileId,
        DietRuleService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.EvaluateAsync(id, profileId, cancellationToken);
            return Results.Ok(result);
        }
        catch (WeeklyPlanNotFoundException ex)
        {
            return Results.NotFound(new { detail = ex.Message });
        }
        catch (DietProfileNotFoundException ex)
        {
            return Results.NotFound(new { detail = ex.Message });
        }
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
