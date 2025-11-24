using Domain.Entities;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.DAL.Repository.Abstractions;

public interface IAnalyticsRepository : IRepository<AnalyticsEvent>
{
    Task AddEventAsync(AnalyticsEvent analyticsEvent);
}