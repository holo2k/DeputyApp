using DeputyApp.Entities;

namespace DeputyApp.DAL.Repository.Abstractions;

public interface IPostRepository : IRepository<Post>
{
    Task<IEnumerable<Post>> GetPublishedAsync(int limit = 50);
}