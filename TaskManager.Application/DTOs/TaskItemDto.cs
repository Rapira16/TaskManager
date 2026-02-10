using TaskManager.Domain.Entities;

namespace TaskManager.Application.DTOs;

public class TaskItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string PriorityName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public Guid ProjectId { get; set; }
    public bool IsOverdue { get; set; }

    public ProjectDto Project { get; set; } = null!;
    public UserDto? AssignedTo { get; set; }
}