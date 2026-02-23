using Domain.Entities;
using Infrastructure.DAL.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DAL.Repository.Implementations;

public class TaskRepository(AppDbContext context) : GenericRepository<TaskEntity>(context), ITaskRepository
{
    public override async Task<TaskEntity?> GetByIdAsync(Guid taskId)
    {
        return await Set.AsNoTracking()
            .Where(t => t.Id == taskId)
            .Include(t => t.Status)
            .Include(t => t.Users)
            .FirstOrDefaultAsync();
    }
}