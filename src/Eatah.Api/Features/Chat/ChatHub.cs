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
}
