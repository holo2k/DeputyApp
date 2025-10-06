namespace DeputyApp.BL.Services.Abstractions;

public interface INotificationService
{
    Task SendTelegramAsync(string chatId, string message);
    Task SendPushAsync(Guid userId, string title, string body);
}