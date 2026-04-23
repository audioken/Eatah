namespace Eatah.Client.Services;

/// <summary>
/// Lightweight toast notifications shown briefly at the top of the screen.
/// Used by features that are not yet implemented to confirm input.
/// </summary>
public sealed class ToastService
{
    private readonly System.Threading.SynchronizationContext? _ctx = System.Threading.SynchronizationContext.Current;
    private CancellationTokenSource? _cts;

    public string? CurrentMessage { get; private set; }

    public event Action? OnChange;

    public void Show(string message, TimeSpan? duration = null)
    {
        _cts?.Cancel();
        var cts = new CancellationTokenSource();
        _cts = cts;
        CurrentMessage = message;
        OnChange?.Invoke();

        var delay = duration ?? TimeSpan.FromSeconds(2.5);
        _ = Task.Delay(delay, cts.Token).ContinueWith(t =>
        {
            if (t.IsCanceled) return;
            CurrentMessage = null;
            OnChange?.Invoke();
        }, TaskScheduler.Default);
    }
}
