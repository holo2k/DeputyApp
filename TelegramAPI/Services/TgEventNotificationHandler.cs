using Domain.Entities;
using Task = System.Threading.Tasks.Task;

namespace Services;

public class TgEventNotificationHandler
{
    private readonly TelegramNotificationService _telegram;
    private readonly HttpClient _httpClient;

    public TgEventNotificationHandler(TelegramNotificationService telegram, IHttpClientFactory httpClientFactory)
    {
        _telegram = telegram;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task OnEventCreatedOrUpdated(string title, string type)
    {
        var internalApiUrl = Environment.GetEnvironmentVariable("API_ADDRESS");

        var chats = await _httpClient.GetFromJsonAsync<List<Chats>>($"{internalApiUrl}/get-chats");

        if (chats == null)
            return;

        var tasks = chats.Select(async chat =>
        {
            try
            {
                await _telegram.SendTelegramAsync(chat.ChatId, $"{type} {title} создано!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке пользователю {chat.ChatId}: {ex.Message}");
            }
        });

        await Task.WhenAll(tasks);
    }

}