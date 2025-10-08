﻿using Domain.Entities;
using Application.Services.Abstractions;
using DeputyApp.DAL.UnitOfWork;

namespace Application.Notifications;

public class EventNotificationHandler
{
    private readonly TelegramNotificationService _telegram;
    private readonly IUnitOfWork _uow;

    public EventNotificationHandler(TelegramNotificationService telegram, IUnitOfWork uow)
    {
        _telegram = telegram;
        _uow = uow;
    }

    public async Task OnEventCreatedOrUpdated(Event e)
    {
        var chats = await _uow.Chats.ListAsync();

        var tasks = chats.Select(async chat =>
        {
            try
            {
                await _telegram.SendTelegramAsync(chat.ChatId, $"Мероприятие {e.Title} создано!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке пользователю {chat.ChatId}: {ex.Message}");
            }
        });

        await Task.WhenAll(tasks);
    }
}