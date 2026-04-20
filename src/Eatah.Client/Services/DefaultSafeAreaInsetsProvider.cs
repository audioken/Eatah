namespace Eatah.Client.Services;

/// <summary>
/// Default provider that returns zero insets.
/// iOS uses CSS env(safe-area-inset-*) natively via viewport-fit=cover.
/// </summary>
public sealed class DefaultSafeAreaInsetsProvider : ISafeAreaInsetsProvider
{
    public Task<(double Top, double Bottom)> GetInsetsAsync() =>
        Task.FromResult((0.0, 0.0));
}
