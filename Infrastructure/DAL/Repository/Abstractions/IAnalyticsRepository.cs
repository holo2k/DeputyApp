using Domain.Entities;

namespace Infrastructure.DAL.Repository.Abstractions;

public interface IAnalyticsRepository : IRepository<AnalyticsEvent>
{
    Task AddEventAsync(AnalyticsEvent analyticsEvent);
}