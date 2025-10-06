using Domain.Entities;

namespace Infrastructure.DAL.Repository.Abstractions;

public interface IFeedbackRepository : IRepository<Feedback>
{
    Task<IEnumerable<Feedback>> RecentAsync(int days = 30);
}