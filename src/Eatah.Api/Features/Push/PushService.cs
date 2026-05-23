using System.Security.Cryptography;
using Eatah.Infrastructure.Persistence;
using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

// Alias to avoid conflict with Eatah.Domain.Entities.PushSubscription
using WebPushSub = Lib.Net.Http.WebPush.PushSubscription;

namespace Eatah.Api.Features.Push;

public interface IPushService
{
    bool IsConfigured { get; }
    Task SendToUserAsync(Guid userId, string title, string body, string? type, string? payload, CancellationToken ct);
}

public class PushService : IPushService
{
    private readonly EatahDbContext _db;
    private readonly PushSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PushService> _logger;

    public PushService(
        EatahDbContext db,
        IOptions<PushSettings> settings,
        IHttpClientFactory httpClientFactory,
        ILogger<PushService> logger)
    {
        _db = db;
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.PublicKey) &&
        !string.IsNullOrWhiteSpace(_settings.PrivateKey);

    public async Task SendToUserAsync(Guid userId, string title, string body, string? type, string? payload, CancellationToken ct)
    {
        if (!IsConfigured)
        {
            _logger.LogDebug("VAPID keys not configured – skipping push for user {UserId}", userId);
            return;
        }

        var subscriptions = await _db.PushSubscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .ToListAsync(ct);

        if (subscriptions.Count == 0) return;

        var pushPayload = System.Text.Json.JsonSerializer.Serialize(new
        {
            title,
            body,
            icon = "/icons/app-icon.svg",
            badge = "/icons/app-icon.svg",
            data = new { type, payload }
        });

        var message = new PushMessage(pushPayload);
        var vapidAuth = new VapidAuthentication(_settings.PublicKey!, _settings.PrivateKey!)
        {
            Subject = _settings.Subject
        };

        var staleIds = new List<Guid>();
        using var httpClient = _httpClientFactory.CreateClient("WebPush");
        var client = new PushServiceClient(httpClient)
        {
            DefaultAuthentication = vapidAuth
        };

        foreach (var sub in subscriptions)
        {
            try
            {
                var webPushSub = new WebPushSub
                {
                    Endpoint = sub.Endpoint,
                    Keys = new Dictionary<string, string>
                    {
                        ["p256dh"] = sub.P256dh,
                        ["auth"] = sub.Auth
                    }
                };
                await client.RequestPushMessageDeliveryAsync(webPushSub, message, ct);
            }
            catch (PushServiceClientException ex) when (
                ex.StatusCode is System.Net.HttpStatusCode.Gone or System.Net.HttpStatusCode.NotFound)
            {
                staleIds.Add(sub.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send push to subscription {Id} for user {UserId}", sub.Id, userId);
            }
        }

        if (staleIds.Count > 0)
        {
            await _db.PushSubscriptions
                .Where(s => staleIds.Contains(s.Id))
                .ExecuteDeleteAsync(ct);
        }
    }

    /// <summary>
    /// Generates a new VAPID EC P-256 key pair and returns them as base64url strings.
    /// Run once to obtain keys to place in configuration / secrets.
    /// </summary>
    public static (string PublicKey, string PrivateKey) GenerateVapidKeys()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var p = ecdsa.ExportParameters(true);

        // Public key: uncompressed point 0x04 || X || Y (65 bytes)
        var pub = new byte[65];
        pub[0] = 0x04;
        p.Q.X!.CopyTo(pub.AsSpan(1));
        p.Q.Y!.CopyTo(pub.AsSpan(33));

        return (Base64UrlEncode(pub), Base64UrlEncode(p.D!));
    }

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
