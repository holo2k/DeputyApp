using Domain.Entities;
using Application.Services.Abstractions;
using DeputyApp.DAL.UnitOfWork;
using Task = System.Threading.Tasks.Task;

namespace Application.Notifications;

public class TgEventNotificationHandler
{
    private readonly TelegramNotificationService _telegram;
    private readonly IUnitOfWork _uow;

    public TgEventNotificationHandler(TelegramNotificationService telegram, IUnitOfWork uow)
    {
        _telegram = telegram;
        _uow = uow;
    }

    public async Task OnEventCreatedOrUpdated(string title, string type)
    {
        var chats = await _uow.Chats.ListAsync();

        var tasks = chats.Select(async chat =>
        {
            try
            {
                await _telegram.SendTelegramAsync(chat.ChatId, $"{type} {title} создано!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке пользователю {chat.ChatId}: {ex.Message}");
            }
        });

        await Task.WhenAll(tasks);
    }
}