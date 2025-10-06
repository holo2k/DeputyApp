using System.Text;
using System.Text.Json;
using Application.Services.Abstractions;

namespace Application.Notifications;

public class TelegramNotificationService(HttpClient http, string botToken, string defaultChatId) : INotificationService
{
    public async Task SendTelegramAsync(string chatId, string message)
    {
        var cid = string.IsNullOrEmpty(chatId) ? defaultChatId : chatId;
        if (string.IsNullOrEmpty(cid)) return;
        var url = $"https://api.telegram.org/bot{botToken}/sendMessage";
        var payload = new { chat_id = cid, text = message, parse_mode = "HTML" };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var r = await http.PostAsync(url, content);
        r.EnsureSuccessStatusCode();
    }

    public Task SendPushAsync(Guid userId, string title, string body)
    {
        return Task.CompletedTask;
    }
}