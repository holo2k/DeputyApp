using Application.Services.Abstractions;
using Application.Services.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Notifications;

public static class EventNotificationInitializer
{
    public static async Task RegisterAsync(IServiceProvider rootProvider)
    {
        await using var scope = rootProvider.CreateAsyncScope();
        var provider = scope.ServiceProvider;

        var eventService = provider.GetRequiredService<IEventService>() as EventService;
        var handler = provider.GetRequiredService<EventNotificationHandler>();

        if (eventService != null)
        {
            eventService.EventCreatedOrUpdated += handler.OnEventCreatedOrUpdated;
            Console.WriteLine("✅ Подписка на EventCreatedOrUpdated выполнена");
        }
        else
        {
            Console.WriteLine("⚠️ Не удалось найти EventService");
        }
    }
}
