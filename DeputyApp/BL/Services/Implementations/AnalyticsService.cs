using DeputyApp.BL.Services.Abstractions;
using DeputyApp.DAL.UnitOfWork;
using DeputyApp.Entities;

namespace DeputyApp.BL.Services.Implementations;

public class AnalyticsService : IAnalyticsService
{
    private readonly IUnitOfWork _uow;

    public AnalyticsService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task TrackAsync(string eventType, Guid? userId, string? payloadJson = null)
    {
        var e = new AnalyticsEvent
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            UserId = userId,
            Timestamp = DateTimeOffset.UtcNow,
            PayloadJson = payloadJson
        };
        await _uow.Analytics.AddEventAsync(e);
        await _uow.SaveChangesAsync();
    }


    public async Task<IEnumerable<AnalyticsEvent>> QueryAsync(DateTimeOffset from, DateTimeOffset to,
        string? eventType = null)
    {
        return await _uow.Analytics.ListAsync(a =>
            a.Timestamp >= from && a.Timestamp <= to && (eventType == null || a.EventType == eventType));
    }
}