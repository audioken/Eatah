using Eatah.Api.Common;
using Eatah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Eatah.Api.Middleware;

/// <summary>
/// Resolves the current workspace for the request. Reads the optional
/// <c>X-Eatah-Workspace</c> header; falls back to the user's Personal workspace.
/// Skips silently if the request is not authenticated.
/// On invalid workspace or missing membership: short-circuits with a 403 ProblemDetails.
/// </summary>
public class WorkspaceResolutionMiddleware
{
    public const string HeaderName = "X-Eatah-Workspace";

    private readonly RequestDelegate _next;

    public WorkspaceResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ICurrentUser currentUser,
        IWorkspaceContext workspaceContext,
        EatahDbContext db)
    {
        // SignalR hub connections don't send X-Eatah-Workspace and don't need workspace
        // context — hub methods (JoinThread, LeaveThread) operate on thread IDs, not workspaces.
        // Skipping here avoids an unnecessary DB round-trip on every WebSocket/SSE/LongPolling request.
        if (context.Request.Path.StartsWithSegments("/hubs"))
        {
            await _next(context);
            return;
        }

        var userId = currentUser.UserId;
        if (userId is null)
        {
            await _next(context);
            return;
        }

        Guid? requestedId = null;
        if (context.Request.Headers.TryGetValue(HeaderName, out var headerValue)
            && Guid.TryParse(headerValue, out var parsed))
        {
            requestedId = parsed;
        }

        if (requestedId is Guid wsId)
        {
            var isMember = await db.WorkspaceMembers
                .AnyAsync(m => m.WorkspaceId == wsId && m.UserId == userId.Value);
            if (!isMember)
            {
                await WriteProblemAsync(context, StatusCodes.Status403Forbidden,
                    ErrorCodes.WorkspaceAccessDenied, "You are not a member of the requested workspace.");
                return;
            }
            workspaceContext.SetCurrent(wsId);
        }
        else
        {
            // Fallback: user's Personal workspace
            var personal = await db.WorkspaceMembers
                .Where(m => m.UserId == userId.Value && m.Workspace.Type == Domain.Entities.WorkspaceType.Personal)
                .Select(m => (Guid?)m.WorkspaceId)
                .FirstOrDefaultAsync();
            if (personal is Guid personalId)
            {
                workspaceContext.SetCurrent(personalId);
            }
            // else: leave unset. Endpoints that require it will fail via RequireCurrent().
        }

        await _next(context);
    }

    private static async Task WriteProblemAsync(HttpContext context, int status, string errorCode, string detail)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        var problem = new
        {
            type = "https://tools.ietf.org/html/rfc7807",
            title = status == 403 ? "Forbidden" : "Bad Request",
            status,
            detail,
            errorCode
        };
        await context.Response.WriteAsJsonAsync(problem);
    }
}

public static class WorkspaceResolutionMiddlewareExtensions
{
    public static IApplicationBuilder UseWorkspaceResolution(this IApplicationBuilder app)
        => app.UseMiddleware<WorkspaceResolutionMiddleware>();
}
