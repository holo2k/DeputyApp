namespace Presentation.Hub;
using Microsoft.AspNetCore.SignalR;

public class NotificationHub : Hub
{
    public async Task SendToAll(string message)
    {
        await Clients.All.SendAsync("SendNotification", message);
    }
    
    public async Task SendNotificationToUsers(string message, params string[] userIds)
    {
        await Clients.Users(userIds).SendAsync("ReceiveNotification", message);
    }
}