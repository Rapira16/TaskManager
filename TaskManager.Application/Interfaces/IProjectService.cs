using TaskManager.Application.DTOs;

namespace TaskManager.Application.Interfaces;

public interface IProjectService
{
    Task<IEnumerable<ProjectDto>> GetAllProjectsAsync();
    Task<ProjectDto?> GetProjectByIdAsync(Guid id);
    Task<ProjectDetailsDto?> GetProjectDetailsAsync(Guid id);
    Task<IEnumerable<ProjectDto>> GetProjectsByOwnerAsync(Guid ownerId);
    Task<ProjectDto> CreateProjectAsync(CreateProjectDto dto);
    Task<bool> UpdateProjectAsync(Guid id, UpdateProjectDto dto);
    Task<bool> DeleteProjectAsync(Guid id);
}