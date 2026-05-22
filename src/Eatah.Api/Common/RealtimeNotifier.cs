using Eatah.Api.Features.Chat;
using Microsoft.AspNetCore.SignalR;

namespace Eatah.Api.Common;

/// <summary>
/// Sends workspace-scoped realtime events to connected clients via SignalR
/// (over the existing <see cref="ChatHub"/>). Clients automatically join
/// <c>workspace:{id}</c> when they connect, so they receive invalidation events
/// for the workspace they currently have selected.
/// <para>
/// Events are intentionally lightweight invalidation signals — receivers refetch
/// the affected resource to pick up the new state. This keeps payloads small and
/// avoids re-broadcasting complex aggregate shapes.
/// </para>
/// </summary>
public interface IRealtimeNotifier
{
    Task ShoppingListChangedAsync(Guid workspaceId, CancellationToken ct = default);
    Task PantryChangedAsync(Guid workspaceId, CancellationToken ct = default);
    Task WeeklyPlanChangedAsync(Guid workspaceId, Guid planId, int year, int weekNumber, CancellationToken ct = default);
}

public sealed class RealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<ChatHub> _hub;

    public RealtimeNotifier(IHubContext<ChatHub> hub)
    {
        _hub = hub;
    }

    public Task ShoppingListChangedAsync(Guid workspaceId, CancellationToken ct = default) =>
        _hub.Clients.Group($"workspace:{workspaceId}")
            .SendAsync("ShoppingListChanged", new { workspaceId }, ct);

    public Task PantryChangedAsync(Guid workspaceId, CancellationToken ct = default) =>
        _hub.Clients.Group($"workspace:{workspaceId}")
            .SendAsync("PantryChanged", new { workspaceId }, ct);

    public Task WeeklyPlanChangedAsync(Guid workspaceId, Guid planId, int year, int weekNumber, CancellationToken ct = default) =>
        _hub.Clients.Group($"workspace:{workspaceId}")
            .SendAsync("WeeklyPlanChanged", new { workspaceId, planId, year, weekNumber }, ct);
}
