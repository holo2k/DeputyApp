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
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
        };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

        _botClient.StartReceiving(
            async (client, update, token) =>
            {
                using var scope = _scopeFactory.CreateScope();
                var messageHandler = scope.ServiceProvider.GetRequiredService<TelegramMessageHandler>();

                if (update.Message != null && update.Message.Text?.StartsWith("/start") == true)
                {
                    await messageHandler.HandleStartCommand(update.Message.Chat.Id);
                    Console.WriteLine($"Added chat {update.Message.Chat.Id}");
                }
                else if (update.CallbackQuery?.Message != null)
                {
                    await messageHandler.HandleStartCommand(update.CallbackQuery.Message.Chat.Id);
                    Console.WriteLine($"Added chat {update.CallbackQuery.Message.Chat.Id}");
                }
            },
            (client, ex, token) =>
            {
                Console.WriteLine($"Polling error: {ex.Message}");
                return Task.CompletedTask;
            },
            receiverOptions,
            cts.Token
        );

        Console.WriteLine("TelegramBotWorker started");

        // Ждём пока токен отмены не сработает, иначе воркер завершится сразу
        await Task.Delay(-1, stoppingToken);
    }
}
