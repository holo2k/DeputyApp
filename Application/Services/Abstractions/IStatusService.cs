using Application.Dtos;
using Domain.Entities;

namespace Application.Services.Abstractions;

public interface IStatusService
{
    Task<Guid> CreateAsync(StatusRequest request);
    Task<StatusResponse> GetByIdAsync(Guid id);
    Task<IEnumerable<StatusResponse>> GetAllAsync();
    Task<Guid> UpdateAsync(Guid id, string newName);
    Task DeleteAsync(Guid id, string newStatusName);
}