using Eatah.Client.Services.Contracts;

namespace Eatah.Client.Services;

/// <summary>
/// Tracks the user's household (each user has at most one). The active workspace ID is
/// sent as the <c>X-Eatah-Workspace</c> header by <see cref="WorkspaceHeaderHandler"/>.
/// Subscribes to the chat hub so household rename events from other members appear live.
/// </summary>
public sealed class WorkspaceState : IDisposable
{
    private readonly ApiClient _api;
    private readonly ChatHubService _hub;
    private WorkspaceResponse? _current;
    private bool _initialized;
    private Guid? _joinedHubGroup;

    public WorkspaceState(ApiClient api, ChatHubService hub)
    {
        _api = api;
        _hub = hub;
        _hub.WorkspaceRenamed += OnWorkspaceRenamed;
        _hub.Reconnected += OnHubReconnected;
    }

    public WorkspaceResponse? Current => _current;
    public Guid? CurrentId => _current?.Id;

    public event Action? OnChange;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_initialized) return;
        await RefreshAsync(ct);
        _initialized = true;
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        try
        {
            var list = await _api.GetWorkspacesAsync(ct);
            SetCurrent(list.FirstOrDefault());
        }
        catch
        {
            // Not authenticated yet — silently ignore
        }
        OnChange?.Invoke();
    }

    /// <summary>Updates the cached household locally (e.g. after rename). Triggers OnChange.</summary>
    public void SetCurrent(WorkspaceResponse? workspace)
    {
        _current = workspace;
        OnChange?.Invoke();
        _ = SyncHubMembershipAsync();
    }

    public void Reset()
    {
        var previous = _joinedHubGroup;
        _current = null;
        _initialized = false;
        OnChange?.Invoke();
        if (previous is Guid id)
        {
            _joinedHubGroup = null;
            _ = _hub.LeaveWorkspaceAsync(id);
        }
    }

    private async Task SyncHubMembershipAsync()
    {
        var target = _current?.Id;
        if (_joinedHubGroup == target) return;

        if (_joinedHubGroup is Guid old)
        {
            await _hub.LeaveWorkspaceAsync(old);
        }
        _joinedHubGroup = target;
        if (target is Guid id)
        {
            await _hub.JoinWorkspaceAsync(id);
        }
    }

    private void OnWorkspaceRenamed(Guid workspaceId, string name)
    {
        if (_current is null || _current.Id != workspaceId) return;
        _current = _current with { Name = name };
        OnChange?.Invoke();
    }

    private void OnHubReconnected()
    {
        // After reconnect, SignalR drops all group memberships server-side. Re-join.
        _joinedHubGroup = null;
        _ = SyncHubMembershipAsync();
    }

    public void Dispose()
    {
        _hub.WorkspaceRenamed -= OnWorkspaceRenamed;
        _hub.Reconnected -= OnHubReconnected;
    }
}
