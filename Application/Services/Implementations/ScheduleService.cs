using System.Linq.Expressions;
using System.Net.Http.Json;
using Application.Services.Abstractions;
using Domain.Entities;
using Domain.GlobalModels;
using Domain.GlobalModels.Abstractions;
using Hangfire;

public class ScheduleService<T> : IScheduleService<T> where T : INotifiable
{
    private readonly IPhoneNotificationService _phoneNotificationService;
    private readonly HttpClient _httpClient;
    private readonly string _telegramApi;

    public ScheduleService(
        IPhoneNotificationService phoneNotificationService,
        IHttpClientFactory httpClientFactory)
    {
        _phoneNotificationService = phoneNotificationService;
        _httpClient = httpClientFactory.CreateClient();
        _telegramApi = Environment.GetEnvironmentVariable("INTERNAL_API") ?? "http://localhost:5001/api/telegram";
    }

    // Уведомление о создании всем
    public void EnqueueCreatedForAll(string jsonEvent, NotificationModel<T> model)
    {
        BackgroundJob.Enqueue(() => SendPhoneToAll(jsonEvent));
        BackgroundJob.Enqueue(() => SendTelegram(model));
    }

    // Планируем напоминания для всех (день/час)
    public void ScheduleRemindersForAll(string jsonEvent, NotificationModel<T> model, DateTimeOffset startAt)
    {
        // телефон
        ScheduleDelayedNotification(() => _phoneNotificationService.SendToAllAsync(jsonEvent), startAt.AddDays(-1));
        ScheduleDelayedNotification(() => _phoneNotificationService.SendToAllAsync(jsonEvent), startAt.AddHours(-1));
        // телеграм
        ScheduleDelayedNotification(() => SendTelegram(model), startAt.AddDays(-1));
        ScheduleDelayedNotification(() => SendTelegram(model), startAt.AddHours(-1));
    }

    // Планируем напоминания для конкретного пользователя
    public void ScheduleRemindersForUser(string userId, string jsonEvent, NotificationModel<T> model, DateTimeOffset startAt)
    {
        ScheduleDelayedNotification(() => _phoneNotificationService.SendToUserAsync(userId, jsonEvent), startAt.AddDays(-1));
        ScheduleDelayedNotification(() => _phoneNotificationService.SendToUserAsync(userId, jsonEvent), startAt.AddHours(-1));

        ScheduleDelayedNotification(() => SendTelegram(model), startAt.AddDays(-1));
        ScheduleDelayedNotification(() => SendTelegram(model), startAt.AddHours(-1));
    }

    // Вспомогательная обёртка
    public Task SendTelegram<T>(NotificationModel<T> model) where T : INotifiable
    {
        var endpoint = model.Notification.TelegramEndpoint;

        return _httpClient.PostAsJsonAsync(
            $"{_telegramApi}/{endpoint}",
            model,
            CancellationToken.None
        );
    }

    // Обёртка для отправки всем телефонам
    public Task SendPhoneToAll(string jsonEvent)
        => _phoneNotificationService.SendToAllAsync(jsonEvent);

    // Универсальная функция планирования
    private void ScheduleDelayedNotification(Expression<Func<Task>> methodCall, DateTimeOffset scheduledTime)
    {
        var delay = scheduledTime - DateTimeOffset.UtcNow;
        if (delay > TimeSpan.Zero)
        {
            BackgroundJob.Schedule(methodCall, delay);
        }
    }
}
