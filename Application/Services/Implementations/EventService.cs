using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Http.Json;
using Application.Notifications;
using Application.Services.Abstractions;
using DeputyApp.DAL.UnitOfWork;
using Domain.Entities;
using Domain.Enums;
using Domain.GlobalModels;
using Hangfire;
using Newtonsoft.Json;
using Task = System.Threading.Tasks.Task;

namespace Application.Services.Implementations;

public class EventService : IEventService
{
    private readonly IUnitOfWork _uow;
    private readonly IPhoneNotificationService _phoneNotificationService;
    private readonly HttpClient _httpClient;
    private readonly string _internalApiUrl;

    public EventService(
        IUnitOfWork uow,
        IPhoneNotificationService phoneNotificationService,
        IHttpClientFactory httpClientFactory)
    {
        _uow = uow;
        _phoneNotificationService = phoneNotificationService;
        _httpClient = httpClientFactory.CreateClient();
        _internalApiUrl = Environment.GetEnvironmentVariable("INTERNAL_API") ?? "http://localhost:5001/api/telegram";
    }

    public async Task<Event> CreateAsync(Event ev)
    {
        ev.Id = Guid.NewGuid();
        ev.CreatedAt = DateTimeOffset.UtcNow;

        await _uow.Events.AddAsync(ev);
        await _uow.SaveChangesAsync();

        var jsonEvent = JsonConvert.SerializeObject(ev);

        // Отправка уведомления через внутреннее API
        var model = new NotificationModel
        {
            Title = ev.Title,
            Type = "Мероприятие"
        };
        await _httpClient.PostAsJsonAsync($"{_internalApiUrl}/send-notify", model);

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

    public async Task AttachDocumentAsync(Guid eventId, Guid documentId, Guid uploadedById, string? description = null)
    {
        var ev = await _uow.Events.GetByIdAsync(eventId);
        if (ev == null)
            throw new KeyNotFoundException("Event not found");

        var doc = await _uow.Documents.GetByIdAsync(documentId);
        if (doc == null)
            throw new KeyNotFoundException("Document not found");

        var attachment = new EventAttachment
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            DocumentId = documentId,
            UploadedById = uploadedById,
            Description = description
        };

        await _uow.EventAttachments.AddAsync(attachment); // нужно добавить репозиторий/таблицу
        await _uow.SaveChangesAsync();
    }

    public async Task RSVPAsync(Guid eventId, Guid userId, AttendeeStatus status, Guid? excuseDocumentId = null, string? excuseNote = null)
    {
        var ev = await _uow.Events.GetByIdAsync(eventId);
        if (ev == null)
            throw new KeyNotFoundException("Event not found");

        var existing = (await _uow.UserEvents.FindAsync(ue => ue.EventId == eventId && ue.UserId == userId)).FirstOrDefault();
        if (existing == null)
        {
            var ue = new UserEvent
            {
                EventId = eventId,
                UserId = userId,
                Status = status,
                ExcuseDocumentId = excuseDocumentId,
                ExcuseNote = excuseNote,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await _uow.UserEvents.AddAsync(ue);
        }
        else
        {
            existing.Status = status;
            existing.ExcuseDocumentId = excuseDocumentId;
            existing.ExcuseNote = excuseNote;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            _uow.UserEvents.Update(existing);
        }

        await _uow.SaveChangesAsync();
    }

    public async Task<IEnumerable<UserEvent>> GetAttendeesAsync(Guid eventId)
    {
        return await _uow.UserEvents.GetByEventIdAsync(eventId);
    }

    public async Task<Event?> GetWithDetailsAsync(Guid id)
    {
        return await _uow.Events.GetWithDetailsAsync(id);
    }
}