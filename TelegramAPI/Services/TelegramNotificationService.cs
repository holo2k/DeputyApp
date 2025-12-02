using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types.Enums;
using TelegramAPI.Services;

public class TelegramNotificationService : INotificationService
{
    private readonly ITelegramBotClient _botClient;
    private readonly string? _defaultChatId;

    public TelegramNotificationService(ITelegramBotClient botClient, string? defaultChatId)
    {
        _botClient = botClient;
        _defaultChatId = defaultChatId;
    }

    public async Task SendTelegramAsync(string chatId, string message)
    {
        var cid = string.IsNullOrEmpty(chatId) ? _defaultChatId : chatId;
        if (string.IsNullOrEmpty(cid)) return;

        try
        {
            var req = new SendMessageRequest
            {
                ParseMode = ParseMode.Html,
                ChatId = cid,
                Text = message
            };
            // возвращаемый тип Message
            await _botClient.SendRequest(req);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SendTelegramAsync failed for {cid}: {ex.Message}");
        }
    }

    public Task SendPushAsync(Guid userId, string title, string body)
    {
        return Task.CompletedTask;
    }
}