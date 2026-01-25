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
    private readonly IScheduleService<Event> _scheduleService;

    public EventService(
        IUnitOfWork uow,
        IPhoneNotificationService phoneNotificationService,
        IScheduleService<Event> scheduleService)
    {
        _uow = uow;
        _phoneNotificationService = phoneNotificationService;
        _scheduleService = scheduleService;
    }

    public async Task<Event> CreateAsync(Event ev)
    {
        ev.Id = Guid.NewGuid();
        ev.CreatedAt = DateTimeOffset.UtcNow;

        await _uow.Events.AddAsync(ev);
        await _uow.SaveChangesAsync();

        // после сохранения ev
        var jsonEvent = JsonConvert.SerializeObject(ev);
        var model = new NotificationModel<Event>
        {
            Notification = ev,
            TargetUserIds = new List<Guid>()
        };

        if (ev.IsPublic)
        {
            // уведомление о создании всем
            _scheduleService.EnqueueCreatedForAll(jsonEvent, model);

            // планирование напоминаний всем
            _scheduleService.ScheduleRemindersForAll(jsonEvent, model, ev.StartAt);
        }
        else
        {
            if (ev.OrganizerId != Guid.Empty)
            {
                model.TargetUserIds.Add((Guid)ev.OrganizerId!);

                // немедленное уведомление организатору
                BackgroundJob.Enqueue(() => _phoneNotificationService.SendToUserAsync(ev.OrganizerId.ToString()!, jsonEvent));
                BackgroundJob.Enqueue(() => _scheduleService.SendTelegram(model));

                // планирование напоминаний организатору
                _scheduleService.ScheduleRemindersForUser(ev.OrganizerId.ToString()!, jsonEvent, model, ev.StartAt);
            }
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
        if (e == null) 
            return;

        _uow.Events.Delete(e);
        await _uow.SaveChangesAsync();
    }

    public async Task<IEnumerable<Event>> GetMyUpcomingAsync(Guid userId, DateTimeOffset from, DateTimeOffset to)
    {
        var events = await _uow.Events.GetMyUpcomingAsync(from, to, userId);
        return events;
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

        await _uow.EventAttachments.AddAsync(attachment);
        await _uow.SaveChangesAsync();
    }

    public async Task RSVPAsync(Guid eventId, Guid userId, AttendeeStatus status, Guid? excuseDocumentId = null, string? excuseNote = null)
    {
        var ev = await _uow.Events.GetByIdAsync(eventId);
        if (ev == null)
            throw new KeyNotFoundException("Event not found");

        var existing = (await _uow.UserEvents.FindAsync(ue => ue.EventId == eventId && ue.UserId == userId)).FirstOrDefault();
        var previousStatus = existing?.Status ?? AttendeeStatus.Unknown;

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

        // Планируем уведомления только при переходе в Yes
        if (status == AttendeeStatus.Yes && previousStatus != AttendeeStatus.Yes)
        {
            var model = new NotificationModel<Event>
            {
                Notification = ev,
                TargetUserIds = new List<Guid> { userId }
            };

            var jsonEvent = JsonConvert.SerializeObject(ev);
            _scheduleService.ScheduleRemindersForUser(userId.ToString(), jsonEvent, model, ev.StartAt);
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