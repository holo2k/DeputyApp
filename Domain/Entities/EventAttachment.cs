namespace Domain.Entities
{
    public class EventAttachment
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public Event Event { get; set; } = null!;

        public Guid DocumentId { get; set; }
        public Document Document { get; set; } = null!;

        public Guid? UploadedById { get; set; }
        public User? UploadedBy { get; set; }

        public string? Description { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
