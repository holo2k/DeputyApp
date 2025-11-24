using Domain.Entities;
using Domain.Enums;
using Task = System.Threading.Tasks.Task;

namespace Application.Services.Abstractions;

public interface IEventService
{
    Task<Event> CreateAsync(Event e);
    Task<IEnumerable<Event>> GetUpcomingAsync(DateTimeOffset from, DateTimeOffset to, int take = 50);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<Event>> GetMyUpcomingAsync(Guid userId, DateTimeOffset from, DateTimeOffset to);
    Task AttachDocumentAsync(Guid eventId, Guid documentId, Guid uploadedById, string? description = null);
    Task RSVPAsync(Guid eventId, Guid userId, AttendeeStatus status, Guid? excuseDocumentId = null, string? excuseNote = null);
    Task<IEnumerable<UserEvent>> GetAttendeesAsync(Guid eventId);
    Task<Event?> GetWithDetailsAsync(Guid id);
}