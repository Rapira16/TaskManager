using System;

namespace TaskManager.Application.DTOs;

public class ProjectDetailsDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    public UserDto Owner { get; set; } = null!;
    
    public List<TaskItemDto> Tasks { get; set; } = new();
    public List<ProjectMemberDto> Members { get; set; } = new();
}

public class ProjectMemberDto
{
    public UserDto User { get; set; } = null!;
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}