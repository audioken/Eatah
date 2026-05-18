using FluentValidation;
using Microsoft.Extensions.Options;

namespace Eatah.Api.Features.AI;

public static class AiEndpoints
{
    public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/dietprofiles/generate", GenerateDietProfile.Handle)
            .WithTags("DietProfiles")
            .RequireAuthorization()
            .WithName(nameof(GenerateDietProfile));

        app.MapPost("/api/ai/meals/generate", GenerateAiMeal.Handle)
            .WithTags("AI")
            .RequireAuthorization()
            .WithName(nameof(GenerateAiMeal));

        return app;
    }
}

public static class AiServiceExtensions
{
    public static IServiceCollection AddAiFeature(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AiSettings>(configuration.GetSection(AiSettings.SectionName));

        services.AddHttpClient<GeminiClient>((provider, client) =>
        {
            var settings = provider.GetRequiredService<IOptions<AiSettings>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, settings.TimeoutSeconds));
        });

        services.AddHttpClient<AnthropicClient>((provider, client) =>
        {
            var settings = provider.GetRequiredService<IOptions<AiSettings>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, settings.TimeoutSeconds));
        });

        services.AddScoped<IAiClient>(sp => new FallbackAiClient(
            sp.GetRequiredService<GeminiClient>(),
            sp.GetRequiredService<AnthropicClient>(),
            sp.GetRequiredService<IOptions<AiSettings>>(),
            sp.GetRequiredService<ILogger<FallbackAiClient>>()));

        services.AddScoped<AiDietRuleGenerator>();
        services.AddScoped<AiMealGenerator>();
        services.AddScoped<IValidator<GenerateDietProfileRequest>, GenerateDietProfileRequestValidator>();
        services.AddScoped<IValidator<GenerateMealRequest>, GenerateMealRequestValidator>();

        return services;
    }
}
