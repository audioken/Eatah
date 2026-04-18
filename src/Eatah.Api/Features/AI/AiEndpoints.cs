using FluentValidation;

namespace Eatah.Api.Features.AI;

public static class AiEndpoints
{
    public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/dietprofiles/generate", GenerateProfile)
            .WithTags("DietProfiles")
            .WithName("GenerateDietProfile");

        app.MapPost("/api/ai/meals/generate", GenerateMeal)
            .WithTags("AI")
            .WithName("GenerateAiMeal");

        return app;
    }

    private static async Task<IResult> GenerateProfile(
        GenerateDietProfileRequest request,
        AiDietRuleGenerator generator,
        IValidator<GenerateDietProfileRequest> validator,
        ILogger<AiDietRuleGenerator> logger,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        try
        {
            var profile = await generator.GenerateAndSaveAsync(request, cancellationToken);
            return Results.Created($"/api/dietprofiles/{profile.Id}", profile);
        }
        catch (AiServiceException ex)
        {
            logger.LogWarning(ex, "AI-generering av kostprofil misslyckades.");
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway,
                title: "AI-tjänsten misslyckades");
        }
    }

    private static async Task<IResult> GenerateMeal(
        GenerateMealRequest request,
        AiMealGenerator generator,
        IValidator<GenerateMealRequest> validator,
        ILogger<AiMealGenerator> logger,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        try
        {
            var meal = await generator.GenerateAsync(request, cancellationToken);
            return Results.Ok(meal);
        }
        catch (AiServiceException ex)
        {
            logger.LogWarning(ex, "AI-generering av maträtt misslyckades.");
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway,
                title: "AI-tjänsten misslyckades");
        }
    }
}

public static class AiServiceExtensions
{
    public static IServiceCollection AddAiFeature(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AiSettings>(configuration.GetSection(AiSettings.SectionName));

        services.AddHttpClient<IAiClient, GeminiClient>((provider, client) =>
        {
            var settings = provider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<AiSettings>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, settings.TimeoutSeconds));
        });

        services.AddScoped<AiDietRuleGenerator>();
        services.AddScoped<AiMealGenerator>();
        services.AddScoped<IValidator<GenerateDietProfileRequest>, GenerateDietProfileRequestValidator>();
        services.AddScoped<IValidator<GenerateMealRequest>, GenerateMealRequestValidator>();

        return services;
    }
}
