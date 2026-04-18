namespace Eatah.Client.Services;

/// <summary>
/// Tracks active asynchronous operations across the app so the UI can show a
/// global loading indicator. Components call <see cref="BeginScope"/> and
/// dispose the returned scope when their work is done.
/// </summary>
public sealed class LoadingState
{
    private int _activeCount;

    public event Action? Changed;

    public bool IsLoading => _activeCount > 0;

    public IDisposable BeginScope()
    {
        Interlocked.Increment(ref _activeCount);
        Changed?.Invoke();
        return new Scope(this);
    }

    private void EndScope()
    {
        if (Interlocked.Decrement(ref _activeCount) < 0)
        {
            Interlocked.Exchange(ref _activeCount, 0);
        }
        Changed?.Invoke();
    }

    private sealed class Scope : IDisposable
    {
        private LoadingState? _owner;

        public Scope(LoadingState owner) => _owner = owner;

        public void Dispose()
        {
            var owner = Interlocked.Exchange(ref _owner, null);
            owner?.EndScope();
        }
    }
}
