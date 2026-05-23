using Microsoft.Extensions.Options;

namespace Eatah.Api.Features.Push;

public static class PushEndpoints
{
    public static void MapPushEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/push")
            .WithTags("Push");

        group.MapGet("/vapid-public-key", GetVapidPublicKey.Handle);
        group.MapPost("/subscribe", Subscribe.Handle).RequireAuthorization();
        group.MapPost("/unsubscribe", Unsubscribe.Handle).RequireAuthorization();
    }

    public static IServiceCollection AddPushFeature(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PushSettings>(configuration.GetSection("VapidSettings"));
        // Named HTTP client for outbound Web Push delivery requests.
        services.AddHttpClient("WebPush");
        services.AddScoped<IPushService, PushService>();
        return services;
    }
}
