using System.Text.Json;
using Eatah.Client.Services;
using Eatah.Client.Services.Contracts;
using Microsoft.JSInterop;

namespace Eatah.WebClient.Services;

/// <summary>
/// Singleton service that subscribes the PWA to Web Push notifications after
/// the user authenticates. Only registers if the browser supports the Push API
/// and the server has VAPID keys configured.
/// </summary>
public sealed class PushNotificationService
{
    private readonly IJSRuntime _js;
    private readonly ApiClient _api;
    private readonly AuthState _auth;
    private bool _initialized;

    public PushNotificationService(IJSRuntime js, ApiClient api, AuthState auth)
    {
        _js = js;
        _api = api;
        _auth = auth;
    }

    /// <summary>
    /// Checks push support, requests permission when needed, and registers the
    /// subscription with the API. Safe to call multiple times — subsequent calls
    /// are no-ops. Must be called from a user-initiated gesture context on first
    /// run (Notification.requestPermission requires a user gesture on some browsers).
    /// </summary>
    public async Task TryInitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;

        if (!_auth.IsAuthenticated) return;

        try
        {
            var supported = await _js.InvokeAsync<bool>("PushNotifications.isSupported");
            if (!supported) return;

            var permission = await _js.InvokeAsync<string>("PushNotifications.getPermission");
            if (permission == "denied") return;

            var vapidKey = await _api.GetVapidPublicKeyAsync();
            if (string.IsNullOrWhiteSpace(vapidKey)) return;

            // Check whether the browser already has an active push subscription.
            var existingJson = await _js.InvokeAsync<string?>("PushNotifications.getExistingSubscription");
            if (existingJson != null)
            {
                // Already subscribed in this browser — sync the subscription to the server.
                await SendSubscriptionToApiAsync(existingJson);
                return;
            }

            // Request permission and create a new subscription.
            string? newJson;
            if (permission == "default")
                newJson = await _js.InvokeAsync<string?>("PushNotifications.requestPermissionAndSubscribe", vapidKey);
            else
                newJson = await _js.InvokeAsync<string?>("PushNotifications.subscribeWithExistingPermission", vapidKey);

            if (newJson != null)
                await SendSubscriptionToApiAsync(newJson);
        }
        catch
        {
            // Best-effort: push subscription failures must never break the app.
        }
    }

    private async Task SendSubscriptionToApiAsync(string json)
    {
        var sub = JsonSerializer.Deserialize<RawPushSubscription>(
            json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (sub is null) return;
        await _api.SubscribePushAsync(new SubscribePushRequest(sub.Endpoint, sub.P256dh, sub.Auth));
    }

    private sealed record RawPushSubscription(string Endpoint, string P256dh, string Auth);
}
