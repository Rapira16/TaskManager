namespace TaskManager.Application.DTOs;

public class CreateTaskDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Status { get; set; } = 0;
    public int Priority { get; set; } = 1;
    public DateTime? DueDate { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public Guid ProjectId { get; set; }
}