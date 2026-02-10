using TaskManager.Application.Common;
using TaskManager.Application.DTOs;

namespace TaskManager.Application.Interfaces;

public interface ITaskService
{
    Task<IEnumerable<TaskItemDto>> GetAllTasksAsync();
    Task<PagedResult<TaskItemDto>> GetPagedTasksAsync(int pageNumber, int pageSize);
    Task<PagedResult<TaskItemDto>> GetPagedTaskWithFilterAsync(TaskQueryParameters parameters);
    Task<TaskItemDto?> GetTaskByIdAsync(Guid id);
    Task<IEnumerable<TaskItemDto>> GetTasksByStatusAsync(int status);
    Task<IEnumerable<TaskItemDto>> GetTasksByPriorityAsync(int priority);
    Task<TaskItemDto> CreateTaskAsync(CreateTaskDto dto);
    Task<bool> UpdateTaskAsync(Guid id, UpdateTaskDto dto);
    Task<bool> DeleteTaskAsync(Guid id);
}