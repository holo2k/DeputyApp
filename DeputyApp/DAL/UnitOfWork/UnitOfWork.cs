using DeputyApp.DAL.Repository.Abstractions;
using DeputyApp.DAL.Repository.Implementations;

namespace DeputyApp.DAL.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;


    public UnitOfWork(AppDbContext db)
    {
        _db = db;
        Users = new UserRepository(db);
        Roles = new RoleRepository(db);
        Posts = new PostRepository(db);
        Events = new EventRepository(db);
        Documents = new DocumentRepository(db);
        Analytics = new AnalyticsRepository(db);
        Feedbacks = new FeedbackRepository(db);
    }

    public IUserRepository Users { get; }
    public IRoleRepository Roles { get; }
    public IPostRepository Posts { get; }
    public IEventRepository Events { get; }
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