using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Eatah.Api.Tests.Integration;

/// <summary>
/// Always authenticates the caller as a fixed test user so integration tests
/// can hit endpoints protected with <c>RequireAuthorization</c> without going
/// through the real login flow.
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public const string TestUserEmail = "test@eatah.local";
    public const string TestUserDisplayName = "TestUser";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId.ToString()),
            new Claim(ClaimTypes.Name, TestUserDisplayName),
            new Claim(ClaimTypes.Email, TestUserEmail)
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
