using System.Net.Http.Json;
using TaskManager.Client.Models;

namespace TaskManager.Client.Services;

public class TaskService : ITaskService
{
    private readonly HttpClient _httpClient;

    public TaskService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PagedResult<TaskItemDto>> GetPagedAsync(
        int page = 1,
        int pageSize = 10,
        int? status = null,
        int? priority = null,
        string? searchTerm = null)
    {
        try
        {
            var url = $"api/tasks/paged?pageNumber={page}&pageSize={pageSize}";

            if (status.HasValue)
                url += $"&status={status}";

            if (priority.HasValue)
                url += $"&priority={priority}";

            if (!string.IsNullOrEmpty(searchTerm))
                url += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";

            var result = await _httpClient.GetFromJsonAsync<PagedResult<TaskItemDto>>(url);
            return result ?? new PagedResult<TaskItemDto>();
        }
        catch
        {
            return new PagedResult<TaskItemDto>();
        }
    }

    public async Task<TaskItemDto?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<TaskItemDto>($"api/tasks/{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<TaskItemDto?> CreateAsync(CreateTaskRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/tasks", request);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<TaskItemDto>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateTaskRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/tasks/{id}", request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/tasks/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}