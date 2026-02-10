namespace TaskManager.Domain.Entities;

public class ProjectMember
{
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public ProjectRole Role { get; set; }
    public DateTime JoinedAt { get; set; }

    public Project Project { get; set; } = null!;
    public User User { get; set; } = null!;
}

public enum ProjectRole
{
    Member = 0,
    Developer = 1,
    Manager = 2,
    Owner = 3
}
