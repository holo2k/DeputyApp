using Domain.Entities;
using Domain.Enums;
using Domain.GlobalModels;
using Infrastructure.DAL.Repository.Abstractions;

public class TgEventNotificationHandler
{
    private readonly TelegramNotificationService _telegram;
    private readonly IChatRepository _chatRepository;

    public TgEventNotificationHandler(TelegramNotificationService telegram, IChatRepository chatRepository)
    {
        _telegram = telegram;
        _chatRepository = chatRepository;
    }

    public async Task OnEventCreatedOrUpdated(NotificationModel<Event> model)
    {
        var internalApiUrl = Environment.GetEnvironmentVariable("API_ADDRESS");

        var chats = await _chatRepository.GetAll();

        if (chats == null)
            return;

        IEnumerable<Chats> targetChats;

        if (model.TargetUserIds != null && model.TargetUserIds.Any())
        {
            // Выбираем только те чаты, где UserId совпадает со списком получателей
            targetChats = chats.Where(c => c.UserId.HasValue && model.TargetUserIds.Contains(c.UserId.Value));
        }
        else
        {
            // Если список пуст — отправляем всем
            targetChats = chats;
        }

        var tasks = targetChats.Select(async chat =>
        {
            string eventType = model.Notification!.Type switch
            {
                EventType.Event => "событии",
                EventType.Meeting => "заседании",
                EventType.Commission => "комиссии",
                _ => ""
            };

            try
            {
                await _telegram.SendTelegramAsync(chat.ChatId, 
                    $"Напоминание о {model.Notification.Type} {model.Notification.Title} - {model.Notification.StartAt.ToString("dd.MM.yyyy HH:mm")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке пользователю {chat.ChatId}: {ex.Message}");
            }
        });

        await Task.WhenAll(tasks);
    }

    public async Task OnEventCreatedOrUpdated(NotificationModel<TaskEntity> model)
    {
        var internalApiUrl = Environment.GetEnvironmentVariable("API_ADDRESS");

        var chats = await _chatRepository.GetAll();

        if (chats == null)
            return;

        IEnumerable<Chats> targetChats;

        if (model.TargetUserIds != null && model.TargetUserIds.Any())
        {
            // Выбираем только те чаты, где UserId совпадает со списком получателей
            targetChats = chats.Where(c => c.UserId.HasValue && model.TargetUserIds.Contains(c.UserId.Value));
        }
        else
        {
            return;
        }

        var tasks = targetChats.Select(async chat =>
        {
            try
            {
                await _telegram.SendTelegramAsync(chat.ChatId,
                    $"Напоминание о скором завершении срока исполнения задачи {model.Notification.Title}!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке пользователю {chat.ChatId}: {ex.Message}");
            }
        });

        await Task.WhenAll(tasks);
    }
}