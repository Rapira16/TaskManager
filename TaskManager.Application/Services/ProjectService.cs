using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TaskManager.Application.DTOs;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Mappings;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMemoryCache _cache;  // ← Добавили
    private readonly ILogger<ProjectService> _logger;

    // Константы для ключей кэша
    private const string AllProjectsCacheKey = "all_projects";
    private const string ProjectCacheKeyPrefix = "project_";
    
    // Время жизни кэша
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public ProjectService(
        IProjectRepository projectRepository,
        IUserRepository userRepository,
        IMemoryCache cache,  // ← Добавили
        ILogger<ProjectService> logger)
    {
        _projectRepository = projectRepository;
        _userRepository = userRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IEnumerable<ProjectDto>> GetAllProjectsAsync()
    {
        // Пытаемся получить из кэша
        if (_cache.TryGetValue(AllProjectsCacheKey, out IEnumerable<ProjectDto>? cachedProjects))
        {
            _logger.LogInformation("Проекты получены из кэша");
            return cachedProjects!;
        }

        _logger.LogInformation("Получение всех проектов из БД");
        var projects = await _projectRepository.GetAllAsync();
        var projectDtos = projects.Select(ProjectMapper.ToDto).ToList();

        // Сохраняем в кэш
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration,
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(AllProjectsCacheKey, projectDtos, cacheOptions);
        _logger.LogInformation("Проекты сохранены в кэш на {Minutes} минут", CacheDuration.TotalMinutes);

        return projectDtos;
    }

    public async Task<ProjectDto?> GetProjectByIdAsync(Guid id)
    {
        var cacheKey = $"{ProjectCacheKeyPrefix}{id}";

        // Пытаемся получить из кэша
        if (_cache.TryGetValue(cacheKey, out ProjectDto? cachedProject))
        {
            _logger.LogInformation("Проект {ProjectId} получен из кэша", id);
            return cachedProject;
        }

        _logger.LogInformation("Получение проекта {ProjectId} из БД", id);
        var project = await _projectRepository.GetByIdAsync(id);
        
        if (project == null)
        {
            _logger.LogWarning("Проект {ProjectId} не найден", id);
            return null;
        }

        var projectDto = ProjectMapper.ToDto(project);

        // Сохраняем в кэш
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration
        };

        _cache.Set(cacheKey, projectDto, cacheOptions);

        return projectDto;
    }

    public async Task<ProjectDetailsDto?> GetProjectDetailsAsync(Guid id)
    {
        // Детали НЕ кэшируем (часто меняются задачи)
        _logger.LogInformation("Получение детальной информации о проекте {ProjectId}", id);
        var project = await _projectRepository.GetByIdWithDetailsAsync(id);
        
        if (project == null)
        {
            _logger.LogWarning("Проект {ProjectId} не найден", id);
            return null;
        }

        return new ProjectDetailsDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            Owner = UserMapper.ToDto(project.Owner),
            Tasks = project.Tasks.Select(TaskMapper.ToDto).ToList(),
            Members = project.Members.Select(m => new ProjectMemberDto
            {
                User = UserMapper.ToDto(m.User),
                Role = m.Role.ToString(),
                JoinedAt = m.JoinedAt
            }).ToList()
        };
    }

    public async Task<IEnumerable<ProjectDto>> GetProjectsByOwnerAsync(Guid ownerId)
    {
        _logger.LogInformation("Получение проектов владельца {OwnerId}", ownerId);
        var projects = await _projectRepository.GetByOwnerIdAsync(ownerId);
        return projects.Select(ProjectMapper.ToDto);
    }

    public async Task<ProjectDto> CreateProjectAsync(CreateProjectDto dto)
    {
        _logger.LogInformation("Создание проекта: {Name}", dto.Name);

        var ownerExists = await _userRepository.ExistsAsync(dto.OwnerId);
        if (!ownerExists)
        {
            _logger.LogWarning("Попытка создать проект с несуществующим владельцем {OwnerId}", dto.OwnerId);
            throw new ArgumentException($"Пользователь с ID {dto.OwnerId} не найден");
        }

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            OwnerId = dto.OwnerId,
            CreatedAt = DateTime.UtcNow
        };

        var createdProject = await _projectRepository.CreateAsync(project);

        // Инвалидируем кэш
        _cache.Remove(AllProjectsCacheKey);
        _logger.LogInformation("Кэш проектов инвалидирован после создания");

        _logger.LogInformation("Создан проект {ProjectId}: {Name}", project.Id, project.Name);
        return ProjectMapper.ToDto(createdProject);
    }

    public async Task<bool> UpdateProjectAsync(Guid id, UpdateProjectDto dto)
    {
        _logger.LogInformation("Обновление проекта {ProjectId}", id);
        
        var project = await _projectRepository.GetByIdAsync(id);
        if (project == null)
        {
            _logger.LogWarning("Попытка обновить несуществующий проект {ProjectId}", id);
            return false;
        }

        project.Name = dto.Name;
        project.Description = dto.Description;
        project.UpdatedAt = DateTime.UtcNow;

        await _projectRepository.UpdateAsync(project);

        // Инвалидируем кэш
        _cache.Remove(AllProjectsCacheKey);
        _cache.Remove($"{ProjectCacheKeyPrefix}{id}");
        _logger.LogInformation("Кэш проектов инвалидирован после обновления");

        _logger.LogInformation("Проект {ProjectId} обновлён", id);
        return true;
    }

    public async Task<bool> DeleteProjectAsync(Guid id)
    {
        _logger.LogInformation("Удаление проекта {ProjectId}", id);
        var result = await _projectRepository.DeleteAsync(id);
        
        if (result)
        {
            // Инвалидируем кэш
            _cache.Remove(AllProjectsCacheKey);
            _cache.Remove($"{ProjectCacheKeyPrefix}{id}");
            _logger.LogInformation("Проект {ProjectId} удалён, кэш инвалидирован", id);
        }
        else
        {
            _logger.LogWarning("Попытка удалить несуществующий проект {ProjectId}", id);
        }
        
        return result;
    }
}