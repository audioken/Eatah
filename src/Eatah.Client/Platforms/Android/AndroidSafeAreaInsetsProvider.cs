using AndroidX.Core.View;
using Eatah.Client.Services;

namespace Eatah.Client.Platforms.Android;

/// <summary>
/// Android implementation that reads the actual system bar insets from the window.
/// Android WebView does not forward window insets as CSS env() variables, so we
/// inject the real values as CSS custom properties from the MAUI layer instead.
/// This handles both gesture navigation and the three-button navigation bar.
/// </summary>
public sealed class AndroidSafeAreaInsetsProvider : ISafeAreaInsetsProvider
{
    public Task<(double Top, double Bottom)> GetInsetsAsync()
    {
        if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window?.DecorView is { } decorView)
        {
            var windowInsets = ViewCompat.GetRootWindowInsets(decorView);
            if (windowInsets is not null)
            {
                var bars = windowInsets.GetInsets(WindowInsetsCompat.Type.SystemBars());
                var density = decorView.Resources?.DisplayMetrics?.Density ?? 1f;
                return Task.FromResult(((double)(bars.Top / density), (double)(bars.Bottom / density)));
            }
        }

        return Task.FromResult((0.0, 0.0));
    }
}
