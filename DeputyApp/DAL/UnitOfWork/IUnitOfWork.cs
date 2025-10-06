using DeputyApp.DAL.Repository.Abstractions;

namespace DeputyApp.DAL.UnitOfWork;

public interface IUnitOfWork : IAsyncDisposable
{
    IUserRepository Users { get; }
    IRoleRepository Roles { get; }
    IPostRepository Posts { get; }
    IEventRepository Events { get; }
    IDocumentRepository Documents { get; }
    IAnalyticsRepository Analytics { get; }
    IFeedbackRepository Feedbacks { get; }


    Task<int> SaveChangesAsync();
}