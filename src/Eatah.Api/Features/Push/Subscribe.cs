using Eatah.Api.Common;
using Eatah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Eatah.Api.Features.Push;

public static class Subscribe
{
    public static async Task<IResult> Handle(
        SubscribePushRequest request,
        ICurrentUser currentUser,
        EatahDbContext db,
        CancellationToken ct)
    {
        if (currentUser.UserId is not Guid userId)
            return Error.Unauthorized(ErrorCodes.AuthNotAuthenticated, "Not authenticated.").ToHttpResult();

        // Upsert: update keys if this endpoint was already registered.
        var existing = await db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == request.Endpoint, ct);

        if (existing is not null)
        {
            existing.P256dh = request.P256dh;
            existing.Auth = request.Auth;
        }
        else
        {
            db.PushSubscriptions.Add(new Domain.Entities.PushSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Endpoint = request.Endpoint,
                P256dh = request.P256dh,
                Auth = request.Auth
            });
        }

        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
