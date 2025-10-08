using Infrastructure.DAL;
using Infrastructure.DAL.Repository.Abstractions;
using Infrastructure.DAL.Repository.Implementations;

namespace DeputyApp.DAL.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;


    public UnitOfWork(AppDbContext db)
    {
        _db = db;
        Catalogs = new CatalogRepository(db);
        Users = new UserRepository(db);
        Roles = new RoleRepository(db);
        Posts = new PostRepository(db);
        Events = new EventRepository(db);
        Documents = new DocumentRepository(db);
        Analytics = new AnalyticsRepository(db);
        Feedbacks = new FeedbackRepository(db);
        Chats = new ChatRepository(db);
    }

    public IUserRepository Users { get; }
    public IRoleRepository Roles { get; }
    public ICatalogRepository Catalogs { get; set; }
    public IPostRepository Posts { get; }
    public IEventRepository Events { get; }
    public IChatRepository Chats { get; }
    public IDocumentRepository Documents { get; }
    public IAnalyticsRepository Analytics { get; }
    public IFeedbackRepository Feedbacks { get; }


    public async Task<int> SaveChangesAsync()
    {
        return await _db.SaveChangesAsync();
    }


    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
    }
}