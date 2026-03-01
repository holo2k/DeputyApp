using Domain.Constants;
using Domain.Entities;
using Infrastructure.DAL.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DAL.Repository.Implementations;

public class StatusRepository(AppDbContext db) : GenericRepository<Status>(db), IStatusRepository
{
    public async Task<Status?> GetByNameAsync(string name)
    {
        return await db.Statuses
            .Include(x => x.TaskEntities)
            .FirstOrDefaultAsync(x => EF.Functions.ILike(x.Name, name));
    }

    public async Task<Status> GetDefaultStatus()
    {
        return await db.Statuses
            .FirstAsync(x => EF.Functions
                .ILike(x.Name,DefaultStatusMapper.ToString(DefaultStatuses.Created)));
    }
}