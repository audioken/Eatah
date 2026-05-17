using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Eatah.WebClient.Services;

/// <summary>
/// Ensures every request to the API includes credentials (cookies) so the
/// authentication cookie is sent on cross-origin requests from the GitHub
/// Pages host to the Render.com API host.
/// </summary>
public class BrowserCredentialsHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        return base.SendAsync(request, cancellationToken);
    }
}
