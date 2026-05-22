using System.Collections.Concurrent;

namespace Eatah.Api.Common;

/// <summary>
/// Serializes expensive multi-row workspace mutations (randomize, shopping-list sync)
/// so two concurrent requests for the same workspace cannot interleave and overwrite
/// each other. Per-process in-memory locks — sufficient for single-instance deployments;
/// the optimistic concurrency tokens (xmin) still protect data integrity across instances.
/// </summary>
public sealed class WorkspaceLockProvider
{
    public const string ScopeRandomize = "randomize";
    public const string ScopeShoppingSync = "shopping_sync";

    private readonly ConcurrentDictionary<(string Scope, Guid WorkspaceId), SemaphoreSlim> _locks = new();

    public async Task<IDisposable> AcquireAsync(string scope, Guid workspaceId, CancellationToken ct)
    {
        var sem = _locks.GetOrAdd((scope, workspaceId), _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(ct);
        return new Releaser(sem);
    }

    private sealed class Releaser : IDisposable
    {
        private SemaphoreSlim? _sem;
        public Releaser(SemaphoreSlim sem) => _sem = sem;
        public void Dispose()
        {
            var s = Interlocked.Exchange(ref _sem, null);
            s?.Release();
        }
    }
}
