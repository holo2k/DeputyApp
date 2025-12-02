using Application.Notifications;
using DeputyApp.DAL.UnitOfWork;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace Presentation.Controllers
{

    [ApiController]
    [Route("api/internal")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class InternalApiController : ControllerBase
    {
        private readonly TelegramMessageHandler _telegramMessageHandler;
        private readonly IUnitOfWork _uow;

        public InternalApiController(TelegramMessageHandler telegramMessageHandler,  IUnitOfWork uow)
        {
            _telegramMessageHandler = telegramMessageHandler;
            _uow = uow;
        }

        [HttpPost("post-message")]
        public async Task<IActionResult> Post([FromBody] Update update)
        {
            if (update == null)
                return Ok();
            if (update.Message != null)
                await _telegramMessageHandler.HandleStartCommand(update.Message.Chat.Id);
            else if (update.CallbackQuery?.Message != null)
                await _telegramMessageHandler.HandleStartCommand(update.CallbackQuery.Message.Chat.Id);
            return Ok();
        }

        [HttpGet("get-chats")]
        public async Task<IActionResult> GetChats()
        {
            var chats = await _uow.Chats.ListAsync();
            return Ok(chats);
        }
    }
}
