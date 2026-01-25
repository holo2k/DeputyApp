using Domain.GlobalModels;
using Domain.GlobalModels.Abstractions;

public interface IScheduleService<T> where T : INotifiable
{
    /// <summary>
    /// Немедленно отправляет уведомление о создании события всем пользователям
    /// (телефон + Telegram). Используется при создании публичного события.
    /// </summary>
    /// <param name="jsonEvent">Сериализованное событие</param>
    /// <param name="model">Модель уведомления</param>
    void EnqueueCreatedForAll(string jsonEvent, NotificationModel<T> model);

    /// <summary>
    /// Планирует напоминания всем пользователям о событии
    /// (за день и за час до начала).
    /// </summary>
    /// <param name="jsonEvent">Сериализованное событие</param>
    /// <param name="model">Модель уведомления</param>
    /// <param name="startAt">Дата и время начала события</param>
    void ScheduleRemindersForAll(string jsonEvent, NotificationModel<T> model, DateTimeOffset startAt);

    /// <summary>
    /// Планирует персональные напоминания конкретному пользователю
    /// (за день и за час до начала события).
    /// </summary>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="jsonEvent">Сериализованное событие</param>
    /// <param name="model">Модель уведомления</param>
    /// <param name="startAt">Дата и время начала события</param>
    void ScheduleRemindersForUser(string userId, string jsonEvent, NotificationModel<T> model, DateTimeOffset startAt);

    /// <summary>
    /// Отправляет уведомление о событии (Event) в Telegram.
    /// Используется как напрямую, так и внутри отложенных задач Hangfire.
    /// </summary>
    /// <param name="model">Модель уведомления</param>
    /// <returns>Асинхронная операция отправки</returns>
    Task SendTelegram<T>(NotificationModel<T> model) where T : INotifiable;

    /// <summary>
    /// Отправляет телефонное уведомление всем пользователям.
    /// Используется для немедленных уведомлений о создании события.
    /// </summary>
    /// <param name="jsonEvent">Сериализованное событие</param>
    /// <returns>Асинхронная операция отправки</returns>
    Task SendPhoneToAll(string jsonEvent);
}
