using Application.Services.Abstractions;
using DeputyApp.DAL.UnitOfWork;
using Domain.Entities;

namespace Application.Services.Implementations;

public class AnalyticsService(IUnitOfWork uow) : IAnalyticsService
{
    public async Task TrackAsync(string eventType, Guid? userId, string? payloadJson = null)
    {
        var e = new AnalyticsEvent
        {
            Id = Guid.NewGuid(), EventType = eventType, UserId = userId, Timestamp = DateTimeOffset.UtcNow,
            PayloadJson = payloadJson
        };
        await uow.Analytics.AddEventAsync(e);
        await uow.SaveChangesAsync();
    }


    public async Task<IEnumerable<AnalyticsEvent>> QueryAsync(DateTimeOffset from, DateTimeOffset to,
        string? eventType = null)
    {
        return await uow.Analytics.ListAsync(a =>
            a.Timestamp >= from && a.Timestamp <= to && (eventType == null || a.EventType == eventType));
    }
}