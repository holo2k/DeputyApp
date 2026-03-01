using Application.Dtos;
using Application.Mappers;
using Application.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("api/task")]
public class TaskController(ITaskService taskService) : ControllerBase
{
    [HttpPost("create")]
    [Authorize]
    public async Task<IActionResult> Create(TaskCreateRequest request)
    {
        var taskId = await taskService.CreateAsync(request);
        return Ok(taskId);
    }

    [HttpPost("update/{taskId}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid taskId, TaskCreateRequest request)
    {
        var task = await taskService.Update(request, taskId);
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
    [Authorize(Roles = "Admin")]
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

    [HttpPost("set-tasks-archived-status/{taskId}")]
    [Authorize]
    public async Task<IActionResult> SetTaskArchivedStatus(Guid taskId, [FromQuery] bool archive)
    {
        return Ok(await taskService.SetArchivedStatus(taskId, archive));
    }

    [HttpPost("add-user-task/{taskId}")]
    [Authorize]
    public async Task<IActionResult> AddUserToTask(Guid taskId, [FromQuery] Guid userId)
    {
        return Ok(await taskService.AddUserAsync(userId, taskId));
    }
    
    [HttpPost("remove-user-task/{taskId}")]
    [Authorize]
    public async Task<IActionResult> RemoveUserFromTask(Guid taskId, [FromQuery] Guid userId)
    {
        return Ok(await taskService.RemoveUserAsync(userId, taskId));
    }

    [HttpGet("get-tasks-by-current-user")]
    [Authorize]
    public async Task<IActionResult> GetTasksByCurrentUser()
    {
        return Ok(await taskService.GetByCurrentUser());
    }

    [HttpGet("get-tasks-by-user/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetTasksByUserId(Guid userId)
    {
        return Ok(await taskService.GetByUserId(userId));
    }

    [HttpGet("get-assigned-tasks")]
    [Authorize]
    public async Task<IActionResult> GetAssignedTasks()
    {
        return Ok(await taskService.GetAssignedTasks());
    }
    
    [HttpGet("get-author-tasks")]
    [Authorize]
    public async Task<IActionResult> GetAuthorTasks()
    {
        return Ok(await taskService.GetAuthorTasks());
    }
}