using TaskManager.Client.Models;

namespace TaskManager.Client.Services;

public interface ITaskService
{
    Task<PagedResult<TaskItemDto>> GetPagedAsync(int page = 1, int pageSize = 10, int? status = null, int? priority = null, string? searchTerm = null);
    Task<TaskItemDto?> GetByIdAsync(Guid id);
    Task<TaskItemDto?> CreateAsync(CreateTaskRequest request);
    Task<bool> UpdateAsync(Guid id, UpdateTaskRequest request);
    Task<bool> DeleteAsync(Guid id);
}