using System;

namespace TaskManager.Application.DTOs;

public class ProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserDto Owner { get; set; } = null!;
    public int TasksCount { get; set; }
    public int MembersCount { get; set; }
}
