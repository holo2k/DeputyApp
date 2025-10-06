using Domain.Entities;

namespace Infrastructure.DAL.Repository.Abstractions;

public interface IPostRepository : IRepository<Post>
{
    Task<IEnumerable<Post>> GetPublishedAsync(int limit = 50);
}