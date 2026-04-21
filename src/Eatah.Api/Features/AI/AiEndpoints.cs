using FluentValidation;

namespace Eatah.Api.Features.AI;

public static class AiEndpoints
{
    public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/dietprofiles/generate", GenerateDietProfile.Handle)
            .WithTags("DietProfiles")
            .WithName(nameof(GenerateDietProfile));

        app.MapPost("/api/ai/meals/generate", GenerateAiMeal.Handle)
            .WithTags("AI")
            .WithName(nameof(GenerateAiMeal));

        return app;
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
