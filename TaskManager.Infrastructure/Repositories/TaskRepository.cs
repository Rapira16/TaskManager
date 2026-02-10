using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Common;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Entities;
using TaskManager.Infrastructure.Data;
using TaskStatus = TaskManager.Domain.Entities.TaskStatus;

namespace TaskManager.Infrastructure.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly ApplicationDbContext _context;

    public TaskRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TaskItem>> GetAllAsync()
    {
        return await _context.Tasks
            .Include(t => t.Project)
            .Include(t => t.AssignedTo)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<PagedResult<TaskItem>> GetPagedAsync(int pageNumber, int pageSize)
    {
        var totalCount = await _context.Tasks.CountAsync();

        var items = await _context.Tasks
            .OrderByDescending(t => t.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<TaskItem>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PagedResult<TaskItem>> GetPagedWithFiltersAsync(TaskQueryParameters parameters)
    {
        var query = _context.Tasks
            .Include(t => t.Project)
            .Include(t => t.AssignedTo)
            .AsQueryable();

        if (parameters.Status.HasValue) query = query.Where(t => (int)t.Status == parameters.Status.Value);
        if (parameters.Priority.HasValue) query = query.Where(t => (int)t.Priority == parameters.Priority.Value);
        if (parameters.ProjectId.HasValue) query = query.Where(t => t.ProjectId == parameters.ProjectId.Value);
        if (parameters.AssignedToUserId.HasValue) query = query.Where(t => t.AssignedToUserId == parameters.AssignedToUserId.Value);
        if (parameters.IsOverdue.HasValue && parameters.IsOverdue.Value)
        {
            var now = DateTime.UtcNow;
            query = query.Where(t =>
                t.DueDate.HasValue &&
                t.DueDate.Value < now &&
                t.Status != TaskStatus.Done);
        }

        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchLower = parameters.SearchTerm.ToLower();
            query = query.Where(t =>
                t.Title.ToLower().Contains(searchLower) ||
                (t.Description != null && t.Description.ToLower().Contains(searchLower)));
        }

        query = parameters.SortBy.ToLower() switch
        {
            "title" => parameters.SortOrder.ToLower() == "asc"
                ? query.OrderBy(t => t.Title)
                : query.OrderByDescending(t => t.Title),

            "priority" => parameters.SortOrder.ToLower() == "asc"
                ? query.OrderBy(t => t.Priority)
                : query.OrderByDescending(t => t.Priority),

            "status" => parameters.SortOrder.ToLower() == "asc"
                ? query.OrderBy(t => t.Status)
                : query.OrderByDescending(t => t.Status),

            "duedate" => parameters.SortOrder.ToLower() == "asc"
                ? query.OrderBy(t => t.DueDate)
                : query.OrderByDescending(t => t.DueDate),

            "updatedat" => parameters.SortOrder.ToLower() == "asc"
                ? query.OrderBy(t => t.UpdatedAt)
                : query.OrderByDescending(t => t.UpdatedAt),

            _ => parameters.SortOrder.ToLower() == "asc"
                ? query.OrderBy(t => t.CreatedAt)
                : query.OrderByDescending(t => t.CreatedAt)
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync();
        
        return new PagedResult<TaskItem>
        {
            Items = items,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id)
    {
        return await _context.Tasks
            .Include(t => t.Project)
            .Include(t => t.AssignedTo)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<TaskItem>> GetByStatusAsync(TaskStatus status)
    {
        return await _context.Tasks
            .Where(t => t.Status == status)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskItem>> GetByPriorityAsync(TaskPriority priority)
    {
        return await _context.Tasks
            .Where(t => t.Priority == priority)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<TaskItem> CreateAsync(TaskItem task)
    {
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        return task;
    }

    public async Task UpdateAsync(TaskItem task)
    {
        _context.Tasks.Update(task);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null) return false;

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
        return true;
    }
}