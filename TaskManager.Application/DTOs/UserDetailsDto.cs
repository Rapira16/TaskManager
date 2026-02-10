namespace TaskManager.Application.DTOs;

public class UserDetailsDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
    
    public int OwnedProjectsCount { get; set; }
    public int AssignedTasksCount { get; set; }
    public int CompletedTasksCount { get; set; }
    
    public List<ProjectDto> OwnedProjects { get; set; } = new();
    public List<TaskItemDto> AssignedTasks { get; set; } = new();
}