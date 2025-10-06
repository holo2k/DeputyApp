using System.Text;
using System.Text.Json;
using DeputyApp.BL.Services.Abstractions;

namespace DeputyApp.BL.Notifications;

public class TelegramNotificationService : INotificationService
{
    private readonly string _botToken;
    private readonly string _defaultChatId;
    private readonly HttpClient _http;

    public TelegramNotificationService(HttpClient http, string botToken, string defaultChatId)
    {
        _http = http;
        _botToken = botToken;
        _defaultChatId = defaultChatId;
    }

    public async Task SendTelegramAsync(string chatId, string message)
    {
        var cid = string.IsNullOrEmpty(chatId) ? _defaultChatId : chatId;
        if (string.IsNullOrEmpty(cid)) return;
        var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
        var payload = new { chat_id = cid, text = message, parse_mode = "HTML" };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var r = await _http.PostAsync(url, content);
        r.EnsureSuccessStatusCode();
    }

    public Task SendPushAsync(Guid userId, string title, string body)
    {
        // Placeholder: push implementation to FCM/APNs can be added.
        return Task.CompletedTask;
    }
}