using TaskManager.Application.DTOs;
using TaskManager.Domain.Entities;
using TaskStatus = TaskManager.Domain.Entities.TaskStatus;

namespace TaskManager.Application.Mappings;

public static class TaskMapper
{
    public static TaskItemDto ToDto(TaskItem task)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));

        bool isOverdue = task.DueDate.HasValue
                            && task.DueDate.Value < DateTime.UtcNow
                            && task.Status != TaskStatus.Done;
        
        return new TaskItemDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = (int)task.Status,
            StatusName = task.Status.ToString(),
            Priority = (int)task.Priority,
            PriorityName = task.Status.ToString(),
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            DueDate = task.DueDate,
            CompletedAt = task.CompletedAt,
            AssignedToUserId = task.AssignedToUserId,
            ProjectId = task.ProjectId,
            IsOverdue = isOverdue,

            Project = ProjectMapper.ToDto(task.Project),
            AssignedTo = task.AssignedTo != null ? UserMapper.ToDto(task.AssignedTo) : null
        };
    }

    public static TaskItem ToEntity(CreateTaskDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        return new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            Status = (TaskStatus)dto.Status,
            Priority = (TaskPriority)dto.Priority,
            DueDate = dto.DueDate,
            AssignedToUserId = dto.AssignedToUserId,
            ProjectId = dto.ProjectId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static void ApplyUpdate(TaskItem task, UpdateTaskDto dto)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var oldStatus = task.Status;

        task.Title = dto.Title;
        task.Description = dto.Description;
        task.Status = (TaskStatus)dto.Status;
        task.Priority = (TaskPriority)dto.Priority;
        task.DueDate = dto.DueDate;
        task.AssignedToUserId = dto.AssignedToUserId;
        task.UpdatedAt = DateTime.UtcNow;

        if (task.Status == TaskStatus.Done && oldStatus != TaskStatus.Done)
        {
            task.CompletedAt = DateTime.UtcNow;
        }
        else if (task.Status != TaskStatus.Done && oldStatus == TaskStatus.Done)
        {
            task.CompletedAt = null;
        }
    }
}