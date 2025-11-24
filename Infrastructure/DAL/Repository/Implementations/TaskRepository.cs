using Domain.Entities;
using Infrastructure.DAL.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DAL.Repository.Implementations;

public class TaskRepository(AppDbContext context) : GenericRepository<TaskEntity>(context), ITaskRepository
{
}