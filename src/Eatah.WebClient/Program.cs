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
builder.Services.AddSingleton<ModalService>();
builder.Services.AddSingleton<ToastService>();
builder.Services.AddSingleton<HeaderState>();
builder.Services.AddSingleton<AuthState>();
builder.Services.AddSingleton<ISafeAreaInsetsProvider, DefaultSafeAreaInsetsProvider>();
builder.Services.AddSingleton<IUserPreferences, LocalStorageUserPreferences>();
builder.Services.AddTransient<LoadingHttpMessageHandler>();

builder.Services.AddHttpClient<ApiClient>(client =>
    {
        client.BaseAddress = WebApiClientOptions.GetBaseAddress(builder.HostEnvironment.BaseAddress);
    })
    .AddHttpMessageHandler<LoadingHttpMessageHandler>();

await builder.Build().RunAsync();
