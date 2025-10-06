using DeputyApp.Entities;

namespace DeputyApp.DAL.Repository.Abstractions;

public interface IAnalyticsRepository : IRepository<AnalyticsEvent>
{
    Task AddEventAsync(AnalyticsEvent analyticsEvent);
}