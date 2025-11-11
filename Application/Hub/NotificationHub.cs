namespace Presentation.Hub;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

[Authorize]
public class NotificationHub : Hub
{
    // При подключении SignalR автоматически знает пользователя по JWT
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        await Groups.AddToGroupAsync(Context.ConnectionId, "common"); // группа общих уведомлений
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{userId}");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "common");
        await base.OnDisconnectedAsync(exception);
    }
}