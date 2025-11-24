using Application.Services.Abstractions;
using Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using Presentation.Hub;
using Task = System.Threading.Tasks.Task;

namespace Application.Services.Implementations;

public class PhoneNotificationService(IHubContext<NotificationHub> _hub) : IPhoneNotificationService
{
    public async Task SendToUserAsync(string userId, string message)
    {
        await _hub.Clients.User(userId).SendAsync("ReceiveNotification", message);
    }

    public async Task SendToAllAsync(string message)
    {
        await _hub.Clients.Group("common").SendAsync("ReceiveNotification", message);
    }
}