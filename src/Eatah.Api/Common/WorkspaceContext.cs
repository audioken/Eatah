namespace Eatah.Api.Common;

/// <summary>
/// Request-scoped workspace context. <see cref="CurrentWorkspaceId"/> is null until
/// <see cref="Eatah.Api.Middleware.WorkspaceResolutionMiddleware"/> resolves it (based on the
/// <c>X-Eatah-Workspace</c> header validated against the user's memberships, or
/// falls back to the user's Personal workspace).
/// </summary>
public interface IWorkspaceContext
{
    Guid? CurrentWorkspaceId { get; }
    void SetCurrent(Guid workspaceId);
    /// <summary>Throws if not set. Use in services that require a workspace.</summary>
    Guid RequireCurrent();
}

public class WorkspaceContext : IWorkspaceContext
{
    public Guid? CurrentWorkspaceId { get; private set; }

    public void SetCurrent(Guid workspaceId) => CurrentWorkspaceId = workspaceId;

    public Guid RequireCurrent() => CurrentWorkspaceId
        ?? throw new InvalidOperationException("Workspace context has not been resolved for this request.");
}
