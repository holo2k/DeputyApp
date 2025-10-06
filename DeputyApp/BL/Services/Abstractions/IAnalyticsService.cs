using DeputyApp.Entities;

namespace DeputyApp.BL.Services.Abstractions;

public interface IAnalyticsService
{
    Task TrackAsync(string eventType, Guid? userId, string? payloadJson = null);
    Task<IEnumerable<AnalyticsEvent>> QueryAsync(DateTimeOffset from, DateTimeOffset to, string? eventType = null);
}