using Application.Dtos;
using Application.Mappers;
using Application.Services.Abstractions;
using DeputyApp.DAL.UnitOfWork;
using Infrastructure.DAL.Repository.Abstractions;

namespace Application.Services.Implementations;

public class StatusService : IStatusService
{
    private readonly IStatusRepository _statusRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IUnitOfWork uow;

    public StatusService(IUnitOfWork uow)
    {
        this.uow = uow;
        _statusRepository = uow.Statuses;
        _taskRepository = uow.Tasks;
    }

    public async Task<Guid> CreateAsync(StatusRequest request)
    {
        var entity = request.ToTaskEntity();
        await _statusRepository.AddAsync(entity);
        await uow.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<StatusResponse> GetByIdAsync(Guid id)
    {
        var status = await _statusRepository.GetByIdAsync(id, include => include.TaskEntities);
        
        ArgumentNullException.ThrowIfNull(status, "Статус с таким идентификатором не найден.");

        return status.ToResponse();
    }

    public async Task<IEnumerable<StatusResponse>> GetAllAsync()
    {
        var statuses = await _statusRepository.ListAsync(includes: include => include.TaskEntities);

        return statuses.Select(x => x.ToResponse());
    }

    public async Task<Guid> UpdateAsync(Guid id, string newName)
    {
        var status = await _statusRepository.GetByIdAsync(id);

        if (status is null)
            throw new ArgumentNullException(nameof(status), "Статус с таким идентификатором не найден.");

        status.Name = newName;

        _statusRepository.Update(status);
        await uow.SaveChangesAsync();

        return id;
    }

    public async Task DeleteAsync(Guid id, string newStatusName)
    {
        var status = await _statusRepository.GetByIdAsync(id, include => include.TaskEntities);

        if (status is null)
            throw new ArgumentNullException(nameof(status), "Статус с таким идентификатором не найден.");

        var newStatus = await _statusRepository.GetByNameAsync(newStatusName.ToLower());
        
        if (newStatus is null)
            throw new ArgumentNullException(nameof(status), "Статус с таким идентификатором не найден.");

        if (newStatus.IsDefault)
            throw new ArgumentException("Статус является стандартным");
        
        if (status.TaskEntities.Any())
        {
            foreach (var task in status.TaskEntities)
            {
                task.Status = await _statusRepository.GetDefaultStatus();
            }
        }

        _statusRepository.Delete(status);
        await uow.SaveChangesAsync();
    }
}
