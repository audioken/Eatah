using Eatah.Api.Common;
using Eatah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Eatah.Api.Features.Push;

public static class Unsubscribe
{
    public static async Task<IResult> Handle(
        UnsubscribePushRequest request,
        ICurrentUser currentUser,
        EatahDbContext db,
        CancellationToken ct)
    {
        if (currentUser.UserId is not Guid userId)
            return Error.Unauthorized(ErrorCodes.AuthNotAuthenticated, "Not authenticated.").ToHttpResult();

        await db.PushSubscriptions
            .Where(s => s.UserId == userId && s.Endpoint == request.Endpoint)
            .ExecuteDeleteAsync(ct);

        return Results.NoContent();
    }
}
