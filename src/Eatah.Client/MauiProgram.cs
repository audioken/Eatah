using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.Logging;
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
		builder.Services.AddSingleton<AiMealCacheService>();
		builder.Services.AddSingleton<IngredientCheckState>();
		builder.Services.AddSingleton<DietProfileState>();
		builder.Services.AddSingleton<PantryStateService>();
		builder.Services.AddSingleton<ShoppingStateService>();
		builder.Services.AddSingleton<WeeklyPlanStateService>();
		builder.Services.AddSingleton<MealsStateService>();
		builder.Services.AddSingleton<PantryCoverageStateService>();
		builder.Services.AddSingleton<ShoppingSyncService>();
		builder.Services.AddSingleton<RealtimeSyncService>();
		builder.Services.AddSingleton<ModalService>();
		builder.Services.AddSingleton<ToastService>();
		builder.Services.AddSingleton<HeaderState>();
		builder.Services.AddSingleton<IUserPreferences, MauiUserPreferences>();
		builder.Services.AddSingleton<ITokenStore, InMemoryTokenStore>();
		builder.Services.AddTransient<TokenAuthorizationHandler>();
		builder.Services.AddTransient<LoadingHttpMessageHandler>();
		builder.Services.AddTransient<WorkspaceHeaderHandler>();

		// Shared cookie jar so the auth cookie survives across requests within the app session.
		builder.Services.AddSingleton(new System.Net.CookieContainer());

#if ANDROID
		builder.Services.AddSingleton<ISafeAreaInsetsProvider, Eatah.Client.Platforms.Android.AndroidSafeAreaInsetsProvider>();
#else
		builder.Services.AddSingleton<ISafeAreaInsetsProvider, DefaultSafeAreaInsetsProvider>();
#endif

		builder.Services.AddHttpClient<ApiClient>(client =>
		{
			client.BaseAddress = ApiClientOptions.GetBaseAddress();
		})
		.ConfigurePrimaryHttpMessageHandler(sp => new HttpClientHandler
		{
			CookieContainer = sp.GetRequiredService<System.Net.CookieContainer>(),
			UseCookies = true,
			AllowAutoRedirect = false
		})
		.AddHttpMessageHandler<TokenAuthorizationHandler>()
		.AddHttpMessageHandler<WorkspaceHeaderHandler>()
		.AddHttpMessageHandler<LoadingHttpMessageHandler>();

		builder.Services.AddSingleton<AuthState>();
		builder.Services.AddSingleton<WorkspaceState>();
		builder.Services.AddSingleton<ChatState>();
		builder.Services.AddSingleton<ChatHubService>(sp => new ChatHubService(
			sp.GetRequiredService<ITokenStore>(),
			ApiClientOptions.GetBaseAddress(),
			sp.GetRequiredService<ILoggerFactory>()));
		builder.Services.AddSingleton<ChatUnreadService>();

		var app = builder.Build();
		// Subscribe to workspace-scoped invalidation events from the chat hub
		// so the pantry/shopping caches stay in sync with mutations from other
		// household members.
		app.Services.GetRequiredService<RealtimeSyncService>().Start();
		app.Services.GetRequiredService<ChatUnreadService>().Start();
		return app;
	}
}
