using Domain.Entities;
using Task = System.Threading.Tasks.Task;

namespace Application.Services.Abstractions;

public interface IAnalyticsService
{
    Task TrackAsync(string eventType, Guid? userId, string? payloadJson = null);
    Task<IEnumerable<AnalyticsEvent>> QueryAsync(DateTimeOffset from, DateTimeOffset to, string? eventType = null);
}