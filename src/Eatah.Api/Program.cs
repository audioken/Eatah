using System.Reflection;
using System.Threading.RateLimiting;
using Eatah.Api.Features.AI;
using Eatah.Api.Features.DietRules;
using Eatah.Api.Features.Meals;
using Eatah.Api.Features.WeeklyPlan;
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

builder.Services.AddMealFeature();
builder.Services.AddWeeklyPlanFeature();
builder.Services.AddDietRuleFeature();
builder.Services.AddAiFeature(builder.Configuration);

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
            .AllowAnyMethod();
        }
        else
        {
            var origins = builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? [];
            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

var dbConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var healthChecks = builder.Services.AddHealthChecks();
if (!string.IsNullOrWhiteSpace(dbConnectionString))
{
    healthChecks.AddNpgSql(dbConnectionString, name: "postgres", tags: new[] { "ready" });
}

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
});

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseCors(CorsPolicyName);
app.UseRateLimiter();

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
}

app.UseHttpsRedirection();

app.MapHealthChecks("/health");

app.MapMealEndpoints();
app.MapWeeklyPlanEndpoints();
app.MapDietRuleEndpoints();
app.MapAiEndpoints();

app.Run();

public partial class Program;
