using Eatah.Client.Services.Contracts;
using Microsoft.AspNetCore.SignalR.Client;

namespace Eatah.Client.Services;

/// <summary>
/// Manages the SignalR connection to the chat hub.
/// Singleton: one connection per app session.
/// </summary>
public sealed class ChatHubService : IAsyncDisposable
{
    private HubConnection? _connection;
    private readonly ITokenStore _tokenStore;

    // Events raised on the UI thread via InvokeAsync when messages arrive
    public event Action<ChatMessageResponse>? MessageReceived;
    public event Action<ChatMessageResponse>? MessageEdited;
    public event Action<Guid>? MessageDeleted;
    public event Action<Guid, IReadOnlyList<ChatReactionGroupResponse>>? ReactionUpdated;

    public HubConnectionState State => _connection?.State ?? HubConnectionState.Disconnected;

    public ChatHubService(ITokenStore tokenStore)
    {
        _tokenStore = tokenStore;
    }

    public async Task StartAsync(string hubUrl, CancellationToken ct = default)
    {
        if (_connection is not null) return;

        var token = _tokenStore.Token;

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, opts =>
            {
                if (!string.IsNullOrEmpty(token))
                    opts.AccessTokenProvider = () => Task.FromResult<string?>(token);
                opts.UseDefaultCredentials = true;
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<ChatMessageResponse>("MessageReceived", msg => MessageReceived?.Invoke(msg));
        _connection.On<ChatMessageResponse>("MessageEdited", msg => MessageEdited?.Invoke(msg));
        _connection.On<Guid>("MessageDeleted", id => MessageDeleted?.Invoke(id));
        _connection.On<ReactionUpdatePayload>("ReactionUpdated", payload =>
            ReactionUpdated?.Invoke(payload.MessageId, payload.Reactions));

        await _connection.StartAsync(ct);
    }

    public async Task JoinThreadAsync(Guid threadId)
    {
        if (_connection?.State == HubConnectionState.Connected)
            await _connection.InvokeAsync("JoinThread", threadId.ToString());
    }

    public async Task LeaveThreadAsync(Guid threadId)
    {
        if (_connection?.State == HubConnectionState.Connected)
            await _connection.InvokeAsync("LeaveThread", threadId.ToString());
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
    }

    private record ReactionUpdatePayload(Guid MessageId, IReadOnlyList<ChatReactionGroupResponse> Reactions);
}
