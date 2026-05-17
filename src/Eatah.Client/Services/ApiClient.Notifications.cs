using System.Net.Http.Json;
using Eatah.Client.Services.Contracts;

namespace Eatah.Client.Services;

public partial class ApiClient
{
    public async Task<List<NotificationResponse>> GetNotificationsAsync(bool unreadOnly = false, CancellationToken ct = default)
    {
        var url = unreadOnly ? "api/notifications?unreadOnly=true" : "api/notifications";
        var response = await _http.GetAsync(url, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<List<NotificationResponse>>(cancellationToken: ct) ?? [];
    }

    public async Task MarkNotificationAsReadAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/notifications/{id}/read", content: null, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task MarkAllNotificationsAsReadAsync(CancellationToken ct = default)
    {
        var response = await _http.PostAsync("api/notifications/read-all", content: null, ct);
        await EnsureSuccessAsync(response, ct);
    }
}
