namespace Eatah.Client.Services;

/// <summary>
/// Injects the <c>X-Eatah-Workspace</c> header from <see cref="WorkspaceState"/>
/// into every outgoing API request so the server resolves the correct workspace.
/// <see cref="WorkspaceState"/> is resolved lazily from <see cref="IServiceProvider"/>
/// to avoid a circular dependency: WorkspaceState → ApiClient → this handler → WorkspaceState.
/// </summary>
public class WorkspaceHeaderHandler : DelegatingHandler
{
    private readonly IServiceProvider _sp;
    private WorkspaceState? _workspace;

    public WorkspaceHeaderHandler(IServiceProvider sp)
    {
        _sp = sp;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _workspace ??= _sp.GetService<WorkspaceState>();
        var id = _workspace?.CurrentId;
        if (id.HasValue)
        {
            request.Headers.TryAddWithoutValidation("X-Eatah-Workspace", id.Value.ToString());
        }
        return base.SendAsync(request, cancellationToken);
    }
}
