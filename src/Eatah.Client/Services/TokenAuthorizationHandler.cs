using System.Net.Http.Headers;

namespace Eatah.Client.Services;

/// <summary>
/// Injects the Bearer token from <see cref="ITokenStore"/> into every outgoing request.
/// If no token is present the request is forwarded unchanged (cookie auth still works for MAUI).
/// </summary>
public class TokenAuthorizationHandler : DelegatingHandler
{
    private readonly ITokenStore _tokenStore;

    public TokenAuthorizationHandler(ITokenStore tokenStore)
    {
        _tokenStore = tokenStore;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _tokenStore.Token;
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
