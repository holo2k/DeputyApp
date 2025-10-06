using DeputyApp.Entities;

namespace DeputyApp.DAL.Repository.Abstractions;

public interface IFeedbackRepository : IRepository<Feedback>
{
    Task<IEnumerable<Feedback>> RecentAsync(int days = 30);
}