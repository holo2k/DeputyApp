using Domain.Enums;

namespace Presentation.Controllers.Requests
{
    public record RsvpRequest(AttendeeStatus Status, Guid? ExcuseDocumentId, string? ExcuseNote);
}
