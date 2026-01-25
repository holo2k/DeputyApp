using System.Net.Http.Json;
using DeputyApp.DAL.UnitOfWork;
using Domain.Entities;
using Domain.GlobalModels;
using Task = System.Threading.Tasks.Task;

namespace Application.Notifications;

public class TelegramMessageHandler
{
    private readonly IUnitOfWork _uow;
    private readonly HttpClient _httpClient;
    private readonly string _telegramApi;

    public TelegramMessageHandler(IUnitOfWork uow, IHttpClientFactory httpClientFactory)
    {
        _uow = uow;
        _httpClient = httpClientFactory.CreateClient();
        _telegramApi = Environment.GetEnvironmentVariable("INTERNAL_API") ?? "http://localhost:5001/api/telegram";
    }

    public async Task HandleStartCommand(long chatIdLong, string? messageText = null)
    {
        var chatId = chatIdLong.ToString();
        var chatRepo = _uow.Chats;
        var chatEntity = await chatRepo.GetByChatId(chatId);

        if (chatEntity == null)
        {
            chatEntity = new Chats
            {
                Id = Guid.NewGuid(),
                ChatId = chatId
            };

            await chatRepo.AddAsync(chatEntity);
            await _uow.SaveChangesAsync();
        }

        // Приветственное сообщение — только если пользователь не привязан
        if (chatEntity.UserId == null)
        {
            var welcomeMessage = new DefaultMessageModel
            {
                ChatId = chatId,
                Message = "Здравствуйте! Для подключения телеграм-бота введите команду /login и через пробел свой адрес электронной почты, привязанный к приложению Кабинет Депутата."
            };

            await _httpClient.PostAsJsonAsync($"{_telegramApi}/send-message", welcomeMessage);
        }

        // Обработка команды /login
        if (!string.IsNullOrWhiteSpace(messageText) &&
            messageText.StartsWith("/login", StringComparison.OrdinalIgnoreCase))
        {
            var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 1)
            {
                var email = parts[1].Trim();
                var user = await _uow.Users.FindByEmailAsync(email);

                if (user != null)
                {
                    chatEntity.UserId = user.Id;
                    await _uow.SaveChangesAsync();

                    var successMessage = new DefaultMessageModel
                    {
                        ChatId = chatId,
                        Message = "Аккаунт успешно привязан."
                    };

                    await _httpClient.PostAsJsonAsync($"{_telegramApi}/send-message", successMessage);
                }
            }
        }
    }
}