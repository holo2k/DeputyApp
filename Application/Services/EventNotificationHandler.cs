using Microsoft.AspNetCore.SignalR;
using Presentation.Hub; 
using Domain.Entities;

namespace Application.Notifications;

public class EventNotificationHandler(IHubContext<NotificationHub> hubContext)
{
    public async Task OnEventCreatedOrUpdated(string title, string type)
    {
        var message = $"{type} '{title}' создано или обновлено";
        await hubContext.Clients.All.SendAsync("ReceiveNotification", message);
    }

    public async Task NotifyUsersAsync(string message, params string[] userIds)
    {
        await hubContext.Clients.Users(userIds).SendAsync("ReceiveNotification", message);
    }

    public async Task NotifyPublicEventAsync(Event e)
    {
        var message = $"Публичное событие '{e.Title}' скоро начнется!";
        await hubContext.Clients.All.SendAsync("ReceiveNotification", message);
    }
}