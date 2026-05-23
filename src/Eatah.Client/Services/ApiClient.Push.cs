using System.Net.Http.Json;
using Eatah.Client.Services.Contracts;

namespace Eatah.Client.Services;

public partial class ApiClient
{
    public async Task<string?> GetVapidPublicKeyAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/push/vapid-public-key", ct);
        if (!response.IsSuccessStatusCode) return null;
        var result = await response.Content.ReadFromJsonAsync<VapidPublicKeyResponse>(cancellationToken: ct);
        return result?.PublicKey;
    }

    public async Task SubscribePushAsync(SubscribePushRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/push/subscribe", request, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task UnsubscribePushAsync(UnsubscribePushRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/push/unsubscribe", request, ct);
        await EnsureSuccessAsync(response, ct);
    }
}
