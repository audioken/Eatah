namespace Eatah.Client.Services;

/// <summary>
/// HTTP message handler that opens a <see cref="LoadingState"/> scope for the
/// duration of every outgoing request, so the global loading indicator reflects
/// any ongoing API call without per-call wiring.
/// </summary>
public sealed class LoadingHttpMessageHandler : DelegatingHandler
{
    private readonly LoadingState _loadingState;

    public LoadingHttpMessageHandler(LoadingState loadingState)
    {
        _loadingState = loadingState;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using (_loadingState.BeginScope())
        {
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
