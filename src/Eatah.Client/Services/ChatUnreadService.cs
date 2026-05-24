namespace Eatah.Client.Services;

/// <summary>
/// Singleton that tracks total unread chat message count across all threads
/// in the currently active workspace. Drives the badge on the chat FAB button.
///
/// Flow:
///  - On workspace change: refetch unread counts from API.
///  - On SignalR <c>ChatUnreadCountChanged</c>: refetch counts (if relevant workspace).
///  - On chat open / thread switch: marks the active thread as read on the server
///    and refreshes local counts.
/// </summary>
public sealed class ChatUnreadService : IDisposable
{
    private readonly ApiClient _api;
    private readonly ChatHubService _hub;
    private readonly WorkspaceState _workspace;
    private readonly ChatState _chatState;

    public int TotalUnread { get; private set; }
    public event Action? OnChanged;

    public ChatUnreadService(ApiClient api, ChatHubService hub, WorkspaceState workspace, ChatState chatState)
    {
        _api = api;
        _hub = hub;
        _workspace = workspace;
        _chatState = chatState;
    }

    public void Start()
    {
        _hub.UnreadCountsChanged += OnUnreadCountsChanged;
        _hub.Reconnected += OnHubReconnected;
        _workspace.OnChange += OnWorkspaceChanged;
        _chatState.OnChanged += OnChatStateChanged;
        _ = RefreshAsync();
    }

    private void OnChatStateChanged()
    {
        // When user opens a thread, mark it as read
        if (_chatState.IsOpen && _chatState.ActiveThread is { } thread)
            _ = MarkReadAsync(thread.Id);
    }

    /// <summary>Mark a thread as read and refresh counts.</summary>
    public async Task MarkReadAsync(Guid threadId)
    {
        try
        {
            await _api.MarkChatThreadReadAsync(threadId);
            await RefreshAsync();
        }
        catch
        {
            // Best-effort — badge will self-correct on next refresh
        }
    }

    private async Task RefreshAsync()
    {
        try
        {
            var counts = await _api.GetChatUnreadCountsAsync();
            TotalUnread = counts.Sum(c => c.UnreadCount);
        }
        catch
        {
            // Network unavailable — keep last known value
        }
        OnChanged?.Invoke();
    }

    private void OnUnreadCountsChanged(Guid workspaceId, Guid _threadId)
    {
        if (_workspace.CurrentId != workspaceId) return;
        // If the chat is open on this thread, mark it as read immediately
        if (_chatState.IsOpen && _chatState.ActiveThread?.Id == _threadId)
            _ = MarkReadAsync(_threadId);
        else
            _ = RefreshAsync();
    }

    private void OnHubReconnected() => _ = RefreshAsync();

    private void OnWorkspaceChanged()
    {
        TotalUnread = 0;
        OnChanged?.Invoke();
        _ = RefreshAsync();
    }

    public void Dispose()
    {
        _hub.UnreadCountsChanged -= OnUnreadCountsChanged;
        _hub.Reconnected -= OnHubReconnected;
        _workspace.OnChange -= OnWorkspaceChanged;
        _chatState.OnChanged -= OnChatStateChanged;
    }
}
