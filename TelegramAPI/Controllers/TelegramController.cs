using Domain.GlobalModels;
using Microsoft.AspNetCore.Mvc;
using Services;
using Telegram.Bot.Types;

namespace TelegramAPI.Controllers;

[ApiController]
[Route("api/telegram")]
public class TelegramController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly TgEventNotificationHandler _tgNotificationHandler;

    public TelegramController(IHttpClientFactory httpClientFactory, TgEventNotificationHandler tgNotificationHandler)
    {
        _httpClient = httpClientFactory.CreateClient();
        _tgNotificationHandler = tgNotificationHandler;
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

    [HttpPost("send-notify")]
    public async Task<IActionResult> OnEventCreatedOrUpdated([FromBody] NotificationModel model)
    {
        await _tgNotificationHandler.OnEventCreatedOrUpdated(model.Title, model.Type);

        return Ok();
    }
}
