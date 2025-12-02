using Application.Dtos;
using Application.Mappers;
using Application.Services.Abstractions;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("api/task")]
public class TaskController(ITaskService taskService) : ControllerBase
{
    [HttpPost("create")]
    [Authorize]
    public async Task<IActionResult> Create(CreateTaskRequest taskRequest)
    {
        var taskId = await taskService.CreateAsync(taskRequest);
        return Ok(taskId);
    }

    [HttpPost("update/{taskId}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid taskId, CreateTaskRequest taskRequest)
    {
        var task = await taskService.Update(taskRequest, taskId);
        return Ok(task);
    }

    [HttpDelete("delete/{taskId}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        await taskService.Delete(id);
        return Ok();
    }

    [HttpGet("get-tasks")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> GetAllTasks()
    {
        return Ok(await taskService.GetAllAsync());
    }

    [HttpGet("get-task/{id}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id)
    {
        return Ok(await taskService.GetByIdAsync(id));
    }

    [HttpPost("set-item-archived-status/{taskId}")]
    [Authorize]
    public async Task<IActionResult> SetItemArchivedStatus(Guid taskId, [FromQuery] bool archive)
    {
        return Ok(await taskService.SetArchivedStatus(taskId, archive));
    }

    [HttpPost("add-user-task/{taskId}")]
    [Authorize]
    public async Task<IActionResult> AddUserToItem(Guid taskId, [FromQuery] Guid userId)
    {
        return Ok(await taskService.AddUserAsync(userId, taskId));
    }

    [HttpGet("get-items-by-current-user")]
    [Authorize]
    public async Task<IActionResult> GetTasksByCurrentUser()
    {
        return Ok(await taskService.GetByCurrentUser());
    }

    [HttpGet("get-items-by-user/{userId}")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> GetTasksByUserId(Guid userId)
    {
        return Ok(await taskService.GetByUserId(userId));
    }
}