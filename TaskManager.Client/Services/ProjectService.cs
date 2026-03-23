using System.Net.Http.Json;
using TaskManager.Client.Models;

namespace TaskManager.Client.Services;

public class ProjectService : IProjectService
{
    private readonly HttpClient _httpClient;

    public ProjectService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ProjectDto>> GetAllAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<ProjectDto>>("api/projects");
            return result ?? new List<ProjectDto>();
        }
        catch
        {
            return new List<ProjectDto>();
        }
    }

    public async Task<ProjectDto?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ProjectDto>($"api/projects/{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<ProjectDetailsDto?> GetDetailsAsync(Guid id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ProjectDetailsDto>($"api/projects/{id}/details");
        }
        catch
        {
            return null;
        }
    }

    public async Task<ProjectDto?> CreateAsync(CreateProjectRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/projects", request);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<ProjectDto>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateProjectRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/projects/{id}", request);
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
            var response = await _httpClient.DeleteAsync($"api/projects/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}