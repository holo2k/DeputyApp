using Domain.Entities;

namespace Infrastructure.DAL.Repository.Abstractions;

public interface IChatRepository : IRepository<Chats>
{
    public Task<Chats> GetByChatId(string id);
    public Task<IEnumerable<Chats>> GetAll();
}