using System.Net.Http.Json;
using Eatah.Client.Services.Contracts;

namespace Eatah.Client.Services;

public partial class ApiClient
{
    public async Task<List<ChatThreadSummaryResponse>> GetChatThreadsAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/chat/threads", ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<List<ChatThreadSummaryResponse>>(cancellationToken: ct) ?? [];
    }

    public async Task<ChatGroupThreadResponse?> GetOrCreateGroupThreadAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/chat/thread", ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<ChatGroupThreadResponse>(cancellationToken: ct);
    }

    public async Task<ChatThreadSummaryResponse?> GetOrCreateDirectThreadAsync(Guid buddyUserId, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/chat/threads/direct",
            new GetOrCreateDirectThreadRequest(buddyUserId), ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<ChatThreadSummaryResponse>(cancellationToken: ct);
    }

    public async Task<List<ChatMessageResponse>> GetChatMessagesAsync(Guid threadId, DateTime? before = null, int take = 50, CancellationToken ct = default)
    {
        var url = $"api/chat/threads/{threadId}/messages?take={take}";
        if (before.HasValue) url += $"&before={Uri.EscapeDataString(before.Value.ToString("O"))}";
        var response = await _http.GetAsync(url, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<List<ChatMessageResponse>>(cancellationToken: ct) ?? [];
    }

    public async Task<ChatMessageResponse?> SendChatMessageAsync(Guid threadId, string text, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"api/chat/threads/{threadId}/messages",
            new SendChatMessageRequest(text), ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<ChatMessageResponse>(cancellationToken: ct);
    }

    public async Task<ChatMessageResponse?> EditChatMessageAsync(Guid messageId, string text, CancellationToken ct = default)
    {
        var response = await _http.PatchAsJsonAsync($"api/chat/messages/{messageId}",
            new EditChatMessageRequest(text), ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<ChatMessageResponse>(cancellationToken: ct);
    }

    public async Task DeleteChatMessageAsync(Guid messageId, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/chat/messages/{messageId}", ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task<List<ChatReactionGroupResponse>?> ToggleChatReactionAsync(Guid messageId, string emoji, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"api/chat/messages/{messageId}/reactions",
            new ToggleChatReactionRequest(emoji), ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<List<ChatReactionGroupResponse>>(cancellationToken: ct);
    }

    public async Task<List<ChatUnreadCountResponse>> GetChatUnreadCountsAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/chat/unread-counts", ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<List<ChatUnreadCountResponse>>(cancellationToken: ct) ?? [];
    }

    public async Task MarkChatThreadReadAsync(Guid threadId, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/chat/threads/{threadId}/mark-read", null, ct);
        await EnsureSuccessAsync(response, ct);
    }
}
