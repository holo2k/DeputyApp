using Domain.Entities;
using Infrastructure.DAL.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DAL.Repository.Implementations;

public class ChatRepository : GenericRepository<Chats>, IChatRepository
{
    private AppDbContext _db;

    public ChatRepository(AppDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Chats>> GetAll()
    {
        return await _db.Chats.ToListAsync();
    }

    public async Task<Chats> GetByChatId(string id)
    {
        return await _db.Chats.FirstOrDefaultAsync(x => x.ChatId == id);
    }
}