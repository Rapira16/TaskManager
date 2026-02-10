using TaskManager.Application.DTOs;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Mappings;
using TaskManager.Domain.Entities;
using Microsoft.Extensions.Logging;
using TaskStatus = TaskManager.Domain.Entities.TaskStatus;
using TaskManager.Application.Common;

namespace TaskManager.Application.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _repository;
    private readonly ILogger<TaskService> _logger;

    public TaskService(ITaskRepository repository, ILogger<TaskService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<TaskItemDto>> GetAllTasksAsync()
    {
        _logger.LogInformation("Получение всех задач");
        var tasks = await _repository.GetAllAsync();
        _logger.LogInformation("Получено {Count} задач", tasks.Count());
        return tasks.Select(TaskMapper.ToDto);
    }

    public async Task<PagedResult<TaskItemDto>> GetPagedTasksAsync(int pageNumber, int pageSize)
    {
        _logger.LogInformation("Получение задач: страница {Page}, размер {Size}", pageNumber, pageSize);
        var pagedResult = await _repository.GetPagedAsync(pageNumber, pageSize);

        return new PagedResult<TaskItemDto>
        {
            Items = pagedResult.Items.Select(TaskMapper.ToDto),
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
            TotalCount = pagedResult.TotalCount
        };
    }

    public async Task<PagedResult<TaskItemDto>> GetPagedTaskWithFilterAsync(TaskQueryParameters parameters)
    {
        _logger.LogInformation(
            "Получение задач с фильтрами: страница {Page}, размер {Size}, статус {Status}, приоритет {Priority}, сортировка {SortBy} {SortOrder}",
            parameters.PageNumber,
            parameters.PageSize,
            parameters.Status,
            parameters.Priority,
            parameters.SortBy,
            parameters.SortOrder);

        if (!parameters.IsValidSortBy())
        {
            _logger.LogWarning("Некорректное поле сортировки: {SortBy}", parameters.SortBy);
            parameters.SortBy = "createdAt";
        }

        if (!parameters.IsValidSortOrder())
        {
            _logger.LogWarning("Некорректный порядок сортировки: {SortOrder}", parameters.SortOrder);
            parameters.SortOrder = "desc";  
        }

        var pagedResult = await _repository.GetPagedWithFiltersAsync(parameters);
        _logger.LogInformation(
            "Получено {Count} задач из {Total} (страница {Page}/{TotalPages})",
            pagedResult.Items.Count(),
            pagedResult.TotalCount,
            pagedResult.PageNumber,
            pagedResult.TotalPages
        );

        return new PagedResult<TaskItemDto>
        {
            Items = pagedResult.Items.Select(TaskMapper.ToDto),
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
            TotalCount = pagedResult.TotalCount
        };
    }

    public async Task<TaskItemDto?> GetTaskByIdAsync(Guid id)
    {
        _logger.LogInformation("Получение задачи {TaskId}", id);
        var task = await _repository.GetByIdAsync(id);

        if (task == null)
        {
            _logger.LogWarning("Задача {TaskId} не найдена", id);
            return null;
        }

        return TaskMapper.ToDto(task);
    }

    public async Task<IEnumerable<TaskItemDto>> GetTasksByStatusAsync(int status)
    {
        _logger.LogInformation("Получение задач со статусом {Status}", status);
        if (!Enum.IsDefined(typeof(TaskStatus), status))
        {
            _logger.LogWarning("Некорректный статус: {Status}", status);
            return Enumerable.Empty<TaskItemDto>();
        }

        var tasks = await _repository.GetByStatusAsync((TaskStatus)status);
        return tasks.Select(TaskMapper.ToDto);
    }

    public async Task<IEnumerable<TaskItemDto>> GetTasksByPriorityAsync(int priority)
    {
        _logger.LogInformation("Получение задач с приоритетом {Priority}", priority);
        if (!Enum.IsDefined(typeof(TaskPriority), priority))
        {
            _logger.LogWarning("Некорректный приоритет: {Priority}", priority);
            return Enumerable.Empty<TaskItemDto>();
        }

        var tasks = await _repository.GetByPriorityAsync((TaskPriority)priority);
        return tasks.Select(TaskMapper.ToDto);
    }

    public async Task<TaskItemDto> CreateTaskAsync(CreateTaskDto dto)
    {
        _logger.LogInformation("Создание новой задачи: {Title}", dto.Title);
        var task = TaskMapper.ToEntity(dto);
        await _repository.CreateAsync(task);
        _logger.LogInformation("Создана задача {TaskId}", task.Id);
        return TaskMapper.ToDto(task);
    }

    public async Task<bool> UpdateTaskAsync(Guid id, UpdateTaskDto dto)
    {
        _logger.LogInformation("Обновление задачи {TaskId}", id);

        var task = await _repository.GetByIdAsync(id);
        if (task == null)
        {
            _logger.LogWarning("Попытка обновить несуществующую задачу {TaskId}", id);
            return false;
        }

        TaskMapper.ApplyUpdate(task, dto);
        await _repository.UpdateAsync(task);
        _logger.LogInformation("Задача {TaskId} обновлена", id);
        return true;
    }

    public async Task<bool> DeleteTaskAsync(Guid id)
    {
        _logger.LogInformation("Удаление задачи {TaskId}", id);

        var result = await _repository.DeleteAsync(id);
        
        if (result)
            _logger.LogInformation("Задача {TaskId} удалена", id);
        else
            _logger.LogWarning("Попытка удалить несуществующую задачу {TaskId}", id);
        
        return result;
    }
}