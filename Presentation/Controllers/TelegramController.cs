using Application.Notifications;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace Presentation.Controllers;

[ApiController]
[Route("api/telegram")]
[ApiExplorerSettings(IgnoreApi = true)]
public class TelegramController : ControllerBase
{
    private readonly TelegramMessageHandler _handler;

    public TelegramController(TelegramMessageHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Update update)
    {
        if (update == null) return Ok();
        if (update.Message != null) await _handler.HandleStartCommand(update.Message.Chat.Id);
        else if (update.CallbackQuery?.Message != null)
            await _handler.HandleStartCommand(update.CallbackQuery.Message.Chat.Id);
        return Ok();
    }
}