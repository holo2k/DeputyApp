using Domain.Entities;

namespace Infrastructure.DAL.Repository.Abstractions;

public interface IStatusRepository : IRepository<Status>
{
    Task<Status?> GetByNameAsync(string name);
    Task<Status> GetDefaultStatus();
}