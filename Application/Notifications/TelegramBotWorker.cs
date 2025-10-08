using Application.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

public class TelegramBotWorker : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IServiceScopeFactory _scopeFactory;

    public TelegramBotWorker(ITelegramBotClient botClient, IServiceScopeFactory scopeFactory)
    {
        _botClient = botClient;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine($"Bot username: {(await _botClient.GetMe()).Username}");

        await _botClient.DeleteWebhook(cancellationToken: stoppingToken);
        Console.WriteLine("Webhook удален, запускаем polling...");

        var receiverOptions = new ReceiverOptions { AllowedUpdates = new[] { UpdateType.Message } };

        _botClient.StartReceiving(
            async (bot, update, token) =>
            {
                Console.WriteLine("Получено обновление!");
                if (update.Message != null && !string.IsNullOrEmpty(update.Message.Text))
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var messageHandler = scope.ServiceProvider.GetRequiredService<TelegramMessageHandler>();
                    await messageHandler.HandleStartCommand(update.Message.Chat.Id);

                }
            },
            async (bot, exception, token) =>
            {
                Console.WriteLine($"Ошибка polling: {exception.Message}");
            },
            receiverOptions,
            stoppingToken
        );

        Console.WriteLine("TelegramBotWorker запущен");
        await Task.Delay(Timeout.Infinite, stoppingToken); 
    }
}