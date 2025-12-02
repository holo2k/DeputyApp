using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Requests;
using Telegram.Bot.Types.Enums;

public class TelegramBotWorker : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly HttpClient _httpClient;
    public TelegramBotWorker(ITelegramBotClient botClient, IServiceScopeFactory scopeFactory, IHttpClientFactory httpClientFactory)
    {
        _botClient = botClient;
        _scopeFactory = scopeFactory;
        _httpClient = httpClientFactory.CreateClient();
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


        var receiverOptions = new ReceiverOptions { AllowedUpdates = new[] { UpdateType.Message } };

        _botClient.StartReceiving(
            async (bot, update, token) =>
            {
                Console.WriteLine("Получено обновление!");
                if (update.Message != null && !string.IsNullOrEmpty(update.Message.Text))
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var internalApiUrl = Environment.GetEnvironmentVariable("API_ADDRESS");
                    await _httpClient.PostAsJsonAsync($"{internalApiUrl}/post-message", update);
                }
            },
            async (bot, exception, token) => { Console.WriteLine($"Ошибка polling: {exception.Message}"); },
            receiverOptions,
            stoppingToken
        );
    }
}