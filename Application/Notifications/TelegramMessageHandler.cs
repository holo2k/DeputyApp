using DeputyApp.DAL.UnitOfWork;
using Domain.Entities;
using Task = System.Threading.Tasks.Task;

namespace Application.Notifications;

public class TelegramMessageHandler
{
    private readonly IUnitOfWork _uow;

    public TelegramMessageHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task HandleStartCommand(long chatId)
    {
        var chatRepo = _uow.Chats;

        var existing = await chatRepo.GetByChatId(chatId.ToString());
        if (existing == null)
        {
            await chatRepo.AddAsync(new Chats
            {
                Id = Guid.NewGuid(),
                ChatId = chatId.ToString()
            });
            await _uow.SaveChangesAsync();
        }
    }
}