using Eatah.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Eatah.Api.Tests.Integration;

public class EatahWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"eatah-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.AddDbContext<EatahDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            // Replace the default authentication scheme with a test handler so
            // [Authorize]/RequireAuthorization endpoints succeed without a real cookie.
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder(TestAuthHandler.SchemeName)
                    .RequireAuthenticatedUser()
                    .Build();
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EatahDbContext>();
        db.Database.EnsureCreated();
        DataSeeder.SeedAsync(db).GetAwaiter().GetResult();
        DataSeeder.EnsureDefaultHouseholdAsync(db, TestAuthHandler.TestUserId).GetAwaiter().GetResult();
        return host;
    }

    public EatahDbContext CreateDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<EatahDbContext>();
    }
}
