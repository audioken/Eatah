using Eatah.Client.Services.Contracts;

namespace Eatah.Client.Services;

/// <summary>
/// Tracks the user's active workspace (Personal or Household) and the list of
/// available workspaces. The active workspace ID is sent as the
/// <c>X-Eatah-Workspace</c> header by <see cref="WorkspaceHeaderHandler"/>.
/// </summary>
public sealed class WorkspaceState
{
    private readonly ApiClient _api;
    private List<WorkspaceResponse> _workspaces = [];
    private Guid? _currentWorkspaceId;
    private bool _initialized;

    public WorkspaceState(ApiClient api)
    {
        _api = api;
    }

    public IReadOnlyList<WorkspaceResponse> Workspaces => _workspaces;

    public WorkspaceResponse? Current =>
        _workspaces.FirstOrDefault(w => w.Id == _currentWorkspaceId)
        ?? _workspaces.FirstOrDefault(w => w.Type == WorkspaceType.Personal);

    public Guid? CurrentId => Current?.Id;

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
            _workspaces = await _api.GetWorkspacesAsync(ct);
            // Default to personal workspace if current is not set or no longer valid
            if (_currentWorkspaceId is null || !_workspaces.Any(w => w.Id == _currentWorkspaceId))
            {
                var personal = _workspaces.FirstOrDefault(w => w.Type == WorkspaceType.Personal);
                _currentWorkspaceId = personal?.Id;
            }
        }
        catch
        {
            // Not authenticated yet — silently ignore
        }
        OnChange?.Invoke();
    }

    public void SwitchTo(Guid workspaceId)
    {
        if (_workspaces.Any(w => w.Id == workspaceId))
        {
            _currentWorkspaceId = workspaceId;
            OnChange?.Invoke();
        }
    }

    public void Reset()
    {
        _workspaces = [];
        _currentWorkspaceId = null;
        _initialized = false;
        OnChange?.Invoke();
    }
}
