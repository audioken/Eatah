using Microsoft.AspNetCore.Components.WebView.Maui;
using Eatah.Client.Services;

namespace Eatah.Client;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();
#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
#endif

		builder.Services.AddSingleton<LoadingState>();
		builder.Services.AddTransient<LoadingHttpMessageHandler>();

#if ANDROID
		builder.Services.AddSingleton<ISafeAreaInsetsProvider, Eatah.Client.Platforms.Android.AndroidSafeAreaInsetsProvider>();
#else
		builder.Services.AddSingleton<ISafeAreaInsetsProvider, DefaultSafeAreaInsetsProvider>();
#endif

		builder.Services.AddHttpClient<ApiClient>(client =>
		{
			client.BaseAddress = ApiClientOptions.GetBaseAddress();
		})
		.AddHttpMessageHandler<LoadingHttpMessageHandler>();

		return builder.Build();
	}
}
