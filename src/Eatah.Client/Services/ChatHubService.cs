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

    /// <summary>Fired after the hub automatically reconnects. Subscribers should re-join their thread groups.</summary>
    public event Action? Reconnected;

    public HubConnectionState State => _connection?.State ?? HubConnectionState.Disconnected;

    public ChatHubService(ITokenStore tokenStore)
    {
        _tokenStore = tokenStore;
    }

    public async Task StartAsync(string hubUrl, CancellationToken ct = default)
    {
        if (_connection is null)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, opts =>
                {
                    // Always read the token dynamically so a token set after the connection
                    // object is built (e.g. after first login) is picked up on connect/reconnect.
                    opts.AccessTokenProvider = () => Task.FromResult<string?>(_tokenStore.Token);
#pragma warning disable CA1416
                    opts.UseDefaultCredentials = true;
#pragma warning restore CA1416
                })
                .WithAutomaticReconnect()
                .Build();

            _connection.On<ChatMessageResponse>("MessageReceived", msg => MessageReceived?.Invoke(msg));
            _connection.On<ChatMessageResponse>("MessageEdited", msg => MessageEdited?.Invoke(msg));
            _connection.On<Guid>("MessageDeleted", id => MessageDeleted?.Invoke(id));
            _connection.On<ReactionUpdatePayload>("ReactionUpdated", payload =>
                ReactionUpdated?.Invoke(payload.MessageId, payload.Reactions));

            // Re-join all thread groups after an automatic reconnect.
            _connection.Reconnected += _ =>
            {
                Reconnected?.Invoke();
                return Task.CompletedTask;
            };
        }

        if (_connection.State == HubConnectionState.Disconnected)
            await _connection.StartAsync(ct);
    }

    public async Task JoinThreadAsync(Guid threadId, CancellationToken ct = default)
    {
        if (_connection is null) return;

        // Wait up to 10 s for the connection to become ready (handles cold-start races).
        for (var i = 0; i < 50 && _connection.State != HubConnectionState.Connected; i++)
            await Task.Delay(200, ct);

        if (_connection.State == HubConnectionState.Connected)
            await _connection.InvokeAsync("JoinThread", threadId.ToString(), ct);
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
