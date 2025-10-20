using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Requests;
using Telegram.Bot.Types.Enums;

namespace Application.Notifications;

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
        try
        {
            // удалить webhook чтобы не было конфликта
            await _botClient.SendRequest(new DeleteWebhookRequest { DropPendingUpdates = true }, stoppingToken);
            var info = await _botClient.SendRequest(new GetWebhookInfoRequest(), stoppingToken);
            Console.WriteLine($"Webhook after delete: {info?.Url ?? "<none>"}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to delete webhook: {ex.Message}");
        }

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
        };

        _botClient.StartReceiving(
            async (client, update, token) =>
            {
                using var scope = _scopeFactory.CreateScope();
                var messageHandler = scope.ServiceProvider.GetRequiredService<TelegramMessageHandler>();

                if (update.Message != null && update.Message.Text?.StartsWith("/start") == true)
                    await messageHandler.HandleStartCommand(update.Message.Chat.Id);
                else if (update.CallbackQuery?.Message != null)
                    await messageHandler.HandleStartCommand(update.CallbackQuery.Message.Chat.Id);
            },
            (client, ex, token) =>
            {
                Console.WriteLine($"Polling error: {ex.Message}");
                return Task.CompletedTask;
            },
            receiverOptions,
            stoppingToken
        );
    }
}