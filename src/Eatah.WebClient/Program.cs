using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Eatah.Client.Services;
using Eatah.WebClient;
using Eatah.WebClient.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<LoadingState>();
builder.Services.AddSingleton<IngredientCheckState>();
builder.Services.AddSingleton<PantryStateService>();
builder.Services.AddSingleton<ShoppingStateService>();
builder.Services.AddSingleton<ShoppingSyncService>();
builder.Services.AddSingleton<ModalService>();
builder.Services.AddSingleton<ToastService>();
builder.Services.AddSingleton<HeaderState>();
builder.Services.AddSingleton<AuthState>();
builder.Services.AddSingleton<WorkspaceState>();
builder.Services.AddSingleton<ChatState>();
builder.Services.AddSingleton<ChatHubService>(sp => new ChatHubService(
    sp.GetRequiredService<ITokenStore>(),
    WebApiClientOptions.GetBaseAddress(builder.HostEnvironment.BaseAddress),
    sp.GetRequiredService<ILoggerFactory>()));
builder.Services.AddSingleton<ISafeAreaInsetsProvider, DefaultSafeAreaInsetsProvider>();
builder.Services.AddSingleton<IUserPreferences, LocalStorageUserPreferences>();
builder.Services.AddTransient<LoadingHttpMessageHandler>();
builder.Services.AddTransient<WorkspaceHeaderHandler>();

// JWT Bearer auth — token is stored in localStorage and injected via TokenAuthorizationHandler.
builder.Services.AddSingleton<ITokenStore, LocalStorageTokenStore>();
builder.Services.AddTransient<TokenAuthorizationHandler>();

builder.Services.AddHttpClient<ApiClient>(client =>
    {
        client.BaseAddress = WebApiClientOptions.GetBaseAddress(builder.HostEnvironment.BaseAddress);
    })
    .AddHttpMessageHandler<TokenAuthorizationHandler>()
    .AddHttpMessageHandler<WorkspaceHeaderHandler>()
    .AddHttpMessageHandler<LoadingHttpMessageHandler>();

await builder.Build().RunAsync();

