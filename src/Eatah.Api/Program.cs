using System.Reflection;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Eatah.Api.Common;
using Microsoft.AspNetCore.HttpOverrides;
using Eatah.Api.Features.AI;
using Eatah.Api.Features.Auth;
using Eatah.Api.Features.Chat;
using Eatah.Api.Features.DietRules;
using Eatah.Api.Features.Friends;
using Eatah.Api.Features.Meals;
using Eatah.Api.Features.Notifications;
using Eatah.Api.Features.Pantry;
using Eatah.Api.Features.WeeklyPlan;
using Eatah.Api.Features.Workspaces;
using Eatah.Api.Middleware;
using Eatah.Infrastructure;
using Eatah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Eatah API",
        Version = "v1",
        Description = "API för veckoplanering av måltider, kostprofiler och AI-genererade kostregler."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddInfrastructure(builder.Configuration);
}

builder.Services.AddAuthFeature(builder.Configuration);

builder.Services.AddWorkspaceFeature();
builder.Services.AddMealFeature();
builder.Services.AddWeeklyPlanFeature();
builder.Services.AddDietRuleFeature();
builder.Services.AddAiFeature(builder.Configuration);
builder.Services.AddNotificationFeature();
builder.Services.AddFriendFeature();
builder.Services.AddPantryFeature();
builder.Services.AddChatFeature();

builder.Services.AddSignalR();

// Realtime broadcast + per-workspace mutation locks.
builder.Services.AddSingleton<WorkspaceLockProvider>();
builder.Services.AddScoped<IRealtimeNotifier, RealtimeNotifier>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// CORS – allow browser clients (Blazor WASM / web app).
// In production, configure Cors:AllowedOrigins in appsettings / environment.
const string CorsPolicyName = "WebClients";
builder.Services.AddCors(cors =>
{
    cors.AddPolicy(CorsPolicyName, policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // Dev: allow any localhost origin so the WASM dev server can connect.
            policy.SetIsOriginAllowed(origin =>
            {
                var uri = new Uri(origin);
                return uri.Host is "localhost" or "127.0.0.1" or "::1";
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        }
        else
        {
            var origins = builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? [];
            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

var dbConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var healthChecks = builder.Services.AddHealthChecks();
if (!string.IsNullOrWhiteSpace(dbConnectionString))
{
    healthChecks.AddNpgSql(dbConnectionString, name: "postgres", tags: new[] { "ready" });
}

// Trust X-Forwarded-* headers from reverse proxies (Render, nginx, etc.) so the
// real client IP is exposed via HttpContext.Connection.RemoteIpAddress instead of
// the proxy's loopback address. Without this, every request from every device looks
// like it comes from the same IP and they all share one rate-limit bucket — leading
// to spurious 429s. KnownNetworks/KnownProxies are intentionally cleared because the
// upstream proxies are not on the same network as the API container.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // Partition by authenticated user when available, otherwise the (now-real) client IP.
    // This prevents members of the same household behind a NAT from sharing a bucket,
    // and gives each user their own quota across devices.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var userId = httpContext.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var key = !string.IsNullOrEmpty(userId)
            ? $"user:{userId}"
            : $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: key,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 300,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    });
});

var app = builder.Build();

// MUST run before authentication and rate limiting so downstream middleware sees
// the real client IP and scheme from X-Forwarded-* headers set by the reverse proxy.
app.UseForwardedHeaders();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseCors(CorsPolicyName);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkspaceResolution();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<EatahDbContext>();
    await dbContext.Database.MigrateAsync();
    await DataSeeder.SeedAsync(dbContext);
    if (app.Environment.IsDevelopment())
    {
        await DataSeeder.SeedDevUserAsync(scope.ServiceProvider);
    }
}

app.UseHttpsRedirection();

app.MapAuthEndpoints();
app.MapHealthChecks("/health");

app.MapWorkspaceEndpoints();
app.MapMealEndpoints();
app.MapWeeklyPlanEndpoints();
app.MapDietRuleEndpoints();
app.MapAiEndpoints();
app.MapNotificationEndpoints();
app.MapFriendEndpoints();
app.MapPantryEndpoints();
app.MapChatEndpoints();
app.MapHub<ChatHub>("/hubs/chat").RequireAuthorization();

app.Run();

public partial class Program;
