using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Eatah.Api.Features.Chat;

[Authorize]
public class ChatHub : Hub
{
    /// <summary>Client calls this after connecting to subscribe to a thread's messages.</summary>
    public Task JoinThread(string threadId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"thread:{threadId}");

    public Task LeaveThread(string threadId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"thread:{threadId}");

    /// <summary>Subscribe to workspace-level events (e.g. household rename). Client calls once per connect.</summary>
    public Task JoinWorkspace(string workspaceId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"workspace:{workspaceId}");

    public Task LeaveWorkspace(string workspaceId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"workspace:{workspaceId}");
}
