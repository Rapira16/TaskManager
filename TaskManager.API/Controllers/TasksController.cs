using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManager.Application.DTOs;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Common;
using TaskManager.Domain.Entities;

namespace TaskManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]  // ← Весь контроллер защищён
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>
    /// Получить все задачи
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskItemDto>>> GetAll()
    {
        var tasks = await _taskService.GetAllTasksAsync();
        return Ok(tasks);
    }

    /// <summary>
    /// Получить задачи с пагинацией, фильтрацией и сортировкой
    /// </summary>
    [HttpGet("paged")]
    public async Task<ActionResult<PagedResult<TaskItemDto>>> GetPaged(
        [FromQuery] TaskQueryParameters parameters)
    {
        var result = await _taskService.GetPagedTaskWithFilterAsync(parameters);
        return Ok(result);
    }

    /// <summary>
    /// Получить задачу по ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<TaskItemDto>> GetById(Guid id)
    {
        var task = await _taskService.GetTaskByIdAsync(id);
        if (task == null)
            return NotFound(new { message = "Задача не найдена" });

        return Ok(task);
    }

    /// <summary>
    /// Создать новую задачу
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TaskItemDto>> Create(CreateTaskDto dto)
    {
        var task = await _taskService.CreateTaskAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }

    /// <summary>
    /// Обновить задачу
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateTaskDto dto)
    {
        var success = await _taskService.UpdateTaskAsync(id, dto);
        if (!success)
            return NotFound(new { message = "Задача не найдена" });

        return NoContent();
    }

    /// <summary>
    /// Удалить задачу (только администраторы)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]  // ← Только админы
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _taskService.DeleteTaskAsync(id);
        if (!success)
            return NotFound(new { message = "Задача не найдена" });

        return NoContent();
    }

    // Остальные методы...
    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<TaskItemDto>>> GetByStatus(int status)
    {
        var tasks = await _taskService.GetTasksByStatusAsync(status);
        return Ok(tasks);
    }

    [HttpGet("priority/{priority}")]
    public async Task<ActionResult<IEnumerable<TaskItemDto>>> GetByPriority(int priority)
    {
        if (!Enum.IsDefined(typeof(TaskPriority), priority))
        {
            return BadRequest(new { message = "Некорректный приоритет" });
        }

        var tasks = await _taskService.GetTasksByPriorityAsync(priority);
        return Ok(tasks);
    }
}