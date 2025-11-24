using Domain.Enums;

namespace Domain.Entities
{
    public class UserEvent
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid EventId { get; set; }
        public Event Event { get; set; } = null!;

        public AttendeeStatus Status { get; set; } = AttendeeStatus.Unknown;

        public Guid? ExcuseDocumentId { get; set; }   // документ с уважительной причиной
        public Document? ExcuseDocument { get; set; }

        public string? ExcuseNote { get; set; }

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
