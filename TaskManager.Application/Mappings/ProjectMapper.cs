using TaskManager.Application.DTOs;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Mappings;

public static class ProjectMapper
{
    public static ProjectDto ToDto(Project project)
    {
        if (project == null) throw new ArgumentNullException(nameof(project));

        return new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CreatedAt = project.CreatedAt,
            Owner = UserMapper.ToDto(project.Owner),
            TasksCount = project.Tasks?.Count ?? 0,
            MembersCount = project.Members?.Count ?? 0
        };
    }
}
