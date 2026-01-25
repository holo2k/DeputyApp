namespace Domain.Entities;

public class Chats
{
    public Guid Id { get; set; }
    public string ChatId { get; set; }
    public Guid? UserId { get; set; }
}