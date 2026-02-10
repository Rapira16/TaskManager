using TaskManager.Application.Common;
using TaskManager.Domain.Entities;
using TaskStatus = TaskManager.Domain.Entities.TaskStatus;

namespace TaskManager.Application.Interfaces;

public interface ITaskRepository
{
    Task<IEnumerable<TaskItem>> GetAllAsync();
    Task<PagedResult<TaskItem>> GetPagedAsync(int pageNumber, int pageSize);
    Task<PagedResult<TaskItem>> GetPagedWithFiltersAsync(TaskQueryParameters parameters);
    Task<TaskItem?> GetByIdAsync(Guid id);
    Task<IEnumerable<TaskItem>> GetByStatusAsync(TaskStatus status);
    Task<IEnumerable<TaskItem>> GetByPriorityAsync(TaskPriority priority);
    Task<TaskItem> CreateAsync(TaskItem task);
    Task UpdateAsync(TaskItem task);
    Task<bool> DeleteAsync(Guid id);
}