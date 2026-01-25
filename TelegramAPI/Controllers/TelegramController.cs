using Domain.Entities;
using Domain.GlobalModels;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace TelegramAPI.Controllers;

[ApiController]
[Route("api/telegram")]
public class TelegramController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly TgEventNotificationHandler _tgNotificationHandler;
    private readonly TelegramNotificationService _telegramNotificationService;

    public TelegramController(IHttpClientFactory httpClientFactory, TgEventNotificationHandler tgNotificationHandler, TelegramNotificationService telegramNotificationService)
    {
        _httpClient = httpClientFactory.CreateClient();
        _tgNotificationHandler = tgNotificationHandler;
        _telegramNotificationService = telegramNotificationService;
    }

    [HttpPost]
    public async Task<IActionResult> HandleStartCommand([FromBody] Update update)
    {
        if (update == null)
            return Ok();

        var internalApiUrl = Environment.GetEnvironmentVariable("API_ADDRESS");
        await _httpClient.PostAsJsonAsync($"{internalApiUrl}/post-message", update);

        return Ok();
    }

    [HttpPost("send-notify-event")]
    public async Task<IActionResult> OnEventCreatedOrUpdated([FromBody] NotificationModel<Event> model)
    {
        await _tgNotificationHandler.OnEventCreatedOrUpdated(model);

        return Ok();
    }

    [HttpPost("send-message")]
    public async Task<IActionResult> OnMessageSended([FromBody] DefaultMessageModel model)
    {
        await _telegramNotificationService.SendTelegramAsync(model.ChatId, model.Message);

        return Ok();
    }
}
