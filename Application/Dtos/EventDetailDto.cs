using Domain.Enums;

namespace Application.Dtos
{
    public class EventDetailDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public EventType Type { get; set; }
        public string? Description { get; set; }
        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset EndAt { get; set; }
        public string? Location { get; set; }
        public bool IsPublic { get; set; }
        public IEnumerable<AttachmentDto> Attachments { get; set; } = Array.Empty<AttachmentDto>();
        public IEnumerable<AttendeeDto> Attendees { get; set; } = Array.Empty<AttendeeDto>();
    }

    public record AttachmentDto(Guid Id, Guid DocumentId, string FileName, string Url, string? Description);
    public record AttendeeDto(Guid UserId, string UserFullName, AttendeeStatus Status, Guid? ExcuseDocumentId, string? ExcuseNote);
}
