namespace Eatah.Client.Services;

/// <summary>
/// Injects the <c>X-Eatah-Workspace</c> header from <see cref="WorkspaceState"/>
/// into every outgoing API request so the server resolves the correct workspace.
/// Registered as a delegating handler in MauiProgram.
/// </summary>
public class WorkspaceHeaderHandler : DelegatingHandler
{
    private readonly WorkspaceState _workspace;

    public WorkspaceHeaderHandler(WorkspaceState workspace)
    {
        _workspace = workspace;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var id = _workspace.CurrentId;
        if (id.HasValue)
        {
            request.Headers.TryAddWithoutValidation("X-Eatah-Workspace", id.Value.ToString());
        }
        return base.SendAsync(request, cancellationToken);
    }
}
