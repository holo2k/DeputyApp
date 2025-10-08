using Application.Services.Abstractions;
using Telegram.Bot;

public class TelegramNotificationService : INotificationService
{
    private readonly ITelegramBotClient _botClient;
    private readonly string _defaultChatId;

    public TelegramNotificationService(ITelegramBotClient botClient, string defaultChatId)
    {
        _botClient = botClient;
        _defaultChatId = defaultChatId;
    }

    public async Task SendTelegramAsync(string chatId, string message)
    {
        var cid = string.IsNullOrEmpty(chatId) ? _defaultChatId : chatId;
        if (string.IsNullOrEmpty(cid)) return;

        await _botClient.SendMessage(cid, message, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
    }

    public Task SendPushAsync(Guid userId, string title, string body) => Task.CompletedTask;
}