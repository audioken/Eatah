using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;

namespace Eatah.Client;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        if (Window is not null)
        {
            // Draw behind status bar and navigation bar (edge-to-edge).
            // Our ISafeAreaInsetsProvider reads the real insets and injects
            // them as CSS custom properties so the Blazor layout can pad correctly.
            WindowCompat.SetDecorFitsSystemWindows(Window, false);

            // Transparent bars so the app background shows through.
            // Android 35+ enforces edge-to-edge with transparent bars by default.
            if (!OperatingSystem.IsAndroidVersionAtLeast(35))
            {
                Window.SetStatusBarColor(Android.Graphics.Color.Transparent);
                Window.SetNavigationBarColor(Android.Graphics.Color.Transparent);
            }

            var controller = WindowCompat.GetInsetsController(Window, Window.DecorView);
            if (controller is not null)
            {
                // White status-bar icons (battery, wifi, clock) for the dark sidebar header.
                controller.AppearanceLightStatusBars = false;

                // Dark navigation-bar icons for the light main content background.
                controller.AppearanceLightNavigationBars = true;
            }
        }
    }
}
