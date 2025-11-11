using Application.Notifications;
using Application.Services.Abstractions;
using DeputyApp.DAL.UnitOfWork;
using Domain.Entities;
using Hangfire;
using Newtonsoft.Json;
using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace Application.Services.Implementations;

public class EventService : IEventService
{
    private readonly IUnitOfWork _uow;
    private readonly TgEventNotificationHandler _tgNotificationHandler;
    private readonly IPhoneNotificationService _phoneNotificationService;

    public EventService(IUnitOfWork uow, TgEventNotificationHandler tgNotificationHandler, IPhoneNotificationService phoneNotificationService)
    {
        _uow = uow;
        _tgNotificationHandler = tgNotificationHandler;
        _phoneNotificationService=phoneNotificationService;
    }

    public async Task<Event> CreateAsync(Event ev)
    {
        ev.Id = Guid.NewGuid();
        ev.CreatedAt = DateTimeOffset.UtcNow;

        await _uow.Events.AddAsync(ev);
        await _uow.SaveChangesAsync();

        var jsonEvent = JsonConvert.SerializeObject(ev);

        // Отправка уведомления в Telegram
        // TODO разделение не личные ивенты
        await _tgNotificationHandler.OnEventCreatedOrUpdated(ev.Title, "Мероприятие");

        // Отправка на телефон
        if (ev.IsPublic)
        {
            ScheduleNotificationsForAll(jsonEvent, ev.StartAt);
        }
        else
        {
            ScheduleNotificationsForUser(ev.OrganizerId.ToString()!, jsonEvent, ev.StartAt);
        }

        return ev;
    }

    public async Task<IEnumerable<Event>> GetUpcomingAsync(DateTimeOffset from, DateTimeOffset to, int take = 50)
    {
        return await _uow.Events.GetUpcomingAsync(from, to);
    }


    public async Task DeleteAsync(Guid id)
    {
        var e = await _uow.Events.GetByIdAsync(id);
        if (e == null) return;
        _uow.Events.Delete(e);
        await _uow.SaveChangesAsync();
    }

    public async Task<IEnumerable<Event>> GetMyUpcomingAsync(Guid userId, DateTimeOffset from, DateTimeOffset to)
    {
        var events = await _uow.Events.GetMyUpcomingAsync(from, to, userId);
        return events;
    }

    private void ScheduleNotificationsForAll(string jsonEvent, DateTimeOffset startAt)
    {
        // Уведомление при создании
        BackgroundJob.Enqueue(() => _phoneNotificationService.SendToAllAsync(jsonEvent));

        // Уведомления за день и за час
        ScheduleDelayedNotification(() => _phoneNotificationService.SendToAllAsync(jsonEvent), startAt.AddDays(-1));
        ScheduleDelayedNotification(() => _phoneNotificationService.SendToAllAsync(jsonEvent), startAt.AddHours(-1));
    }

    private void ScheduleNotificationsForUser(string userId, string jsonEvent, DateTimeOffset startAt)
    {
        // Уведомление при создании
        BackgroundJob.Enqueue(() => _phoneNotificationService.SendToUserAsync(userId, jsonEvent));

        // Уведомления за день и за час
        ScheduleDelayedNotification(() => _phoneNotificationService.SendToUserAsync(userId, jsonEvent), startAt.AddDays(-1));
        ScheduleDelayedNotification(() => _phoneNotificationService.SendToUserAsync(userId, jsonEvent), startAt.AddHours(-1));
    }

    private void ScheduleDelayedNotification(Expression<Func<Task>> methodCall, DateTimeOffset scheduledTime)
    {
        var delay = scheduledTime - DateTimeOffset.UtcNow;

        // Планируем только если событие ещё не прошло
        if (delay > TimeSpan.Zero)
        {
            BackgroundJob.Schedule(methodCall, delay);
        }
        // Если событие уже наступило или почти наступило — уведомление не планируем
    }
}