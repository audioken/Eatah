using Eatah.Api.Common;
using Microsoft.Extensions.Options;

namespace Eatah.Api.Features.Push;

public static class GetVapidPublicKey
{
    public static IResult Handle(IOptions<PushSettings> settings)
    {
        var key = settings.Value.PublicKey;
        if (string.IsNullOrWhiteSpace(key))
            return Error.BadRequest(ErrorCodes.PushNotConfigured, "Push notifications are not configured on the server.").ToHttpResult();
        return Results.Ok(new VapidPublicKeyResponse(key));
    }
}
