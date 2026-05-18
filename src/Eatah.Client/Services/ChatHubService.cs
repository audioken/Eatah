using Eatah.Client.Services.Contracts;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Eatah.Client.Services;

/// <summary>
/// Manages the SignalR connection to the chat hub.
/// Singleton: one connection per app session.
/// Resilient against cold starts, dropped sockets (mobile background) and
/// initial connect failures — automatic background retry until connected.
/// </summary>
public sealed class ChatHubService : IAsyncDisposable
{
    private static readonly TimeSpan[] ReconnectDelays =
    {
        TimeSpan.Zero,
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(60),
        TimeSpan.FromSeconds(60),
    };

    private HubConnection? _connection;
    private readonly ITokenStore _tokenStore;
    private readonly Uri _apiBaseAddress;
    private readonly ILogger<ChatHubService> _logger;
    private readonly SemaphoreSlim _startLock = new(1, 1);
    private CancellationTokenSource? _backgroundRetryCts;

    // Events raised on the UI thread via InvokeAsync when messages arrive
    public event Action<ChatMessageResponse>? MessageReceived;
    public event Action<ChatMessageResponse>? MessageEdited;
    public event Action<Guid>? MessageDeleted;
    public event Action<Guid, IReadOnlyList<ChatReactionGroupResponse>>? ReactionUpdated;

    /// <summary>Fired after the hub (re)connects. Subscribers should re-join their thread groups.</summary>
    public event Action? Reconnected;

    public HubConnectionState State => _connection?.State ?? HubConnectionState.Disconnected;
    public bool IsConnected => State == HubConnectionState.Connected;

    public ChatHubService(ITokenStore tokenStore, Uri apiBaseAddress, ILoggerFactory loggerFactory)
    {
        _tokenStore = tokenStore;
        _apiBaseAddress = apiBaseAddress;
        _logger = loggerFactory.CreateLogger<ChatHubService>();
    }

    /// <summary>
    /// Ensures the connection exists and is started. Safe to call repeatedly.
    /// If the initial connect fails it schedules a background retry — the caller
    /// does not need to retry. Returns true if connected when the method returns.
    /// </summary>
    public async Task<bool> EnsureConnectedAsync(CancellationToken ct = default)
    {
        EnsureConnectionBuilt();
        if (_connection!.State == HubConnectionState.Connected) return true;

        await _startLock.WaitAsync(ct);
        try
        {
            if (_connection.State == HubConnectionState.Connected) return true;
            if (_connection.State == HubConnectionState.Disconnected)
            {
                try
                {
                    _logger.LogInformation("ChatHub: starting connection");
                    await _connection.StartAsync(ct);
                    _logger.LogInformation("ChatHub: connected");
                    SafeRaiseReconnected();
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ChatHub: initial connect failed, scheduling background retry");
                    StartBackgroundRetry();
                    return false;
                }
            }
        }
        finally
        {
            _startLock.Release();
        }

        // Connecting / Reconnecting — wait briefly for it to settle.
        for (var i = 0; i < 25 && _connection.State != HubConnectionState.Connected; i++)
            await Task.Delay(200, ct);
        return _connection.State == HubConnectionState.Connected;
    }

    public async Task JoinThreadAsync(Guid threadId, CancellationToken ct = default)
    {
        var connected = await EnsureConnectedAsync(ct);
        if (!connected || _connection is null) return;

        try
        {
            await _connection.InvokeAsync("JoinThread", threadId.ToString(), ct);
            _logger.LogDebug("ChatHub: joined thread {ThreadId}", threadId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ChatHub: JoinThread failed for {ThreadId}", threadId);
        }
    }

    public async Task LeaveThreadAsync(Guid threadId)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            try { await _connection.InvokeAsync("LeaveThread", threadId.ToString()); }
            catch (Exception ex) { _logger.LogDebug(ex, "ChatHub: LeaveThread failed"); }
        }
    }

    private void EnsureConnectionBuilt()
    {
        if (_connection is not null) return;

        var hubUrl = new Uri(_apiBaseAddress, "hubs/chat").ToString();

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, opts =>
            {
                // Read the token dynamically so a token set after the connection
                // is built (e.g. after first login) is picked up on connect/reconnect.
                opts.AccessTokenProvider = () => Task.FromResult<string?>(_tokenStore.Token);
                // UseDefaultCredentials sends the Windows/cookie credential automatically.
                // It is only supported on MAUI/desktop — browser (WASM) throws PlatformNotSupportedException.
#pragma warning disable CA1416
                try { opts.UseDefaultCredentials = true; }
                catch (PlatformNotSupportedException) { /* browser/WASM — not supported, ignored */ }
#pragma warning restore CA1416
            })
            .WithAutomaticReconnect(ReconnectDelays)
            .ConfigureLogging(lb => lb.SetMinimumLevel(LogLevel.Warning))
            .Build();

        // Detect dead sockets faster (default is 30s/60s — too slow for mobile background kills).
        _connection.KeepAliveInterval = TimeSpan.FromSeconds(10);
        _connection.ServerTimeout = TimeSpan.FromSeconds(30);
        _connection.HandshakeTimeout = TimeSpan.FromSeconds(30);

        _connection.On<ChatMessageResponse>("MessageReceived", msg => MessageReceived?.Invoke(msg));
        _connection.On<ChatMessageResponse>("MessageEdited", msg => MessageEdited?.Invoke(msg));
        _connection.On<Guid>("MessageDeleted", id => MessageDeleted?.Invoke(id));
        _connection.On<ReactionUpdatePayload>("ReactionUpdated", payload =>
            ReactionUpdated?.Invoke(payload.MessageId, payload.Reactions));

        _connection.Reconnecting += ex =>
        {
            _logger.LogInformation(ex, "ChatHub: reconnecting");
            return Task.CompletedTask;
        };

        _connection.Reconnected += _ =>
        {
            _logger.LogInformation("ChatHub: reconnected");
            SafeRaiseReconnected();
            return Task.CompletedTask;
        };

        _connection.Closed += ex =>
        {
            _logger.LogWarning(ex, "ChatHub: closed, scheduling background retry");
            // WithAutomaticReconnect gave up (or never engaged). Restart ourselves.
            StartBackgroundRetry();
            return Task.CompletedTask;
        };
    }

    private void StartBackgroundRetry()
    {
        _backgroundRetryCts?.Cancel();
        _backgroundRetryCts = new CancellationTokenSource();
        var ct = _backgroundRetryCts.Token;

        _ = Task.Run(async () =>
        {
            var delay = TimeSpan.FromSeconds(2);
            var maxDelay = TimeSpan.FromSeconds(60);

            while (!ct.IsCancellationRequested)
            {
                try { await Task.Delay(delay, ct); }
                catch (OperationCanceledException) { return; }

                if (_connection is null || _connection.State == HubConnectionState.Connected) return;
                if (_connection.State != HubConnectionState.Disconnected) continue;

                try
                {
                    _logger.LogDebug("ChatHub: background retry attempt");
                    await _connection.StartAsync(ct);
                    _logger.LogInformation("ChatHub: background retry succeeded");
                    SafeRaiseReconnected();
                    return;
                }
                catch (OperationCanceledException) { return; }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "ChatHub: background retry failed, will retry");
                    delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 1.5, maxDelay.TotalSeconds));
                }
            }
        }, ct);
    }

    private void SafeRaiseReconnected()
    {
        try { Reconnected?.Invoke(); }
        catch (Exception ex) { _logger.LogWarning(ex, "ChatHub: Reconnected handler threw"); }
    }

    public async ValueTask DisposeAsync()
    {
        _backgroundRetryCts?.Cancel();
        _backgroundRetryCts?.Dispose();
        if (_connection is not null)
            await _connection.DisposeAsync();
        _startLock.Dispose();
    }

    private record ReactionUpdatePayload(Guid MessageId, IReadOnlyList<ChatReactionGroupResponse> Reactions);
}
