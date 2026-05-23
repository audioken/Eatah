namespace Eatah.Api.Features.Push;

public record SubscribePushRequest(string Endpoint, string P256dh, string Auth);
public record UnsubscribePushRequest(string Endpoint);
public record VapidPublicKeyResponse(string PublicKey);
