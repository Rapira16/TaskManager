using TaskManager.Domain.Entities;

namespace TaskManager.Application.Interfaces;

public interface IProjectRepository
{
    Task<IEnumerable<Project>> GetAllAsync();
    Task<Project?> GetByIdAsync(Guid id);
    Task<Project?> GetByIdWithDetailsAsync(Guid id);
    Task<IEnumerable<Project>> GetByOwnerIdAsync(Guid ownerId);
    Task<Project> CreateAsync(Project project);
    Task UpdateAsync(Project project);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}