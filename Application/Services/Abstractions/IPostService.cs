using Domain.Entities;

namespace Application.Services.Abstractions;

public interface IPostService
{
    Task<Post?> GetByIdAsync(Guid id);
    Task<IEnumerable<Post>> GetPublishedAsync(int skip = 0, int take = 20);
    Task<Post> CreateAsync(Post post);
    Task PublishAsync(Guid postId);
    Task DeleteAsync(Guid postId);
}