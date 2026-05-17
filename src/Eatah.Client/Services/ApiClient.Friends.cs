using System.Net.Http.Json;
using Eatah.Client.Services.Contracts;

namespace Eatah.Client.Services;

public partial class ApiClient
{
    public async Task<List<UserSearchResult>> SearchUsersAsync(string query, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/users/search?q={Uri.EscapeDataString(query)}", ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<List<UserSearchResult>>(cancellationToken: ct) ?? [];
    }

    public async Task<List<FriendResponse>> GetFriendsAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/friends", ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<List<FriendResponse>>(cancellationToken: ct) ?? [];
    }

    public async Task<List<FriendRequestResponse>> GetPendingFriendRequestsAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/friends/requests/incoming", ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<List<FriendRequestResponse>>(cancellationToken: ct) ?? [];
    }

    public async Task<FriendRequestResponse?> SendFriendRequestAsync(SendFriendRequestRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/friends/requests", request, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<FriendRequestResponse>(cancellationToken: ct);
    }

    public async Task RespondToFriendRequestAsync(Guid requestId, bool accept, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"api/friends/requests/{requestId}/respond",
            new RespondToFriendRequestRequest(accept), ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task CancelFriendRequestAsync(Guid requestId, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/friends/requests/{requestId}", ct);
        await EnsureSuccessAsync(response, ct);
    }
}
