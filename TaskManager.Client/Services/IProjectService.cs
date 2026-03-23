using TaskManager.Client.Models;

namespace TaskManager.Client.Services;

public interface IProjectService
{
    Task<List<ProjectDto>> GetAllAsync();
    Task<ProjectDto?> GetByIdAsync(Guid id);
    Task<ProjectDetailsDto?> GetDetailsAsync(Guid id);
    Task<ProjectDto?> CreateAsync(CreateProjectRequest request);
    Task<bool> UpdateAsync(Guid id, UpdateProjectRequest request);
    Task<bool> DeleteAsync(Guid id);
}