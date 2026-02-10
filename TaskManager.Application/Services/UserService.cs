using Microsoft.Extensions.Logging;
using TaskManager.Application.DTOs;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Mappings;
using TaskManager.Domain.Entities;
using TaskStatus = TaskManager.Domain.Entities.TaskStatus;

namespace TaskManager.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        _logger.LogInformation("Получение всех пользователей");
        var users = await _userRepository.GetAllAsync();
        return users.Select(UserMapper.ToDto);
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id)
    {
        _logger.LogInformation("Получение пользователя {UserId}", id);
        var user = await _userRepository.GetByIdAsync(id);
        
        if (user == null)
        {
            _logger.LogWarning("Пользователь {UserId} не найден", id);
            return null;
        }

        return UserMapper.ToDto(user);
    }

    public async Task<UserDetailsDto?> GetUserDetailsAsync(Guid id)
    {
        _logger.LogInformation("Получение детальной информации о пользователе {UserId}", id);
        var user = await _userRepository.GetByIdWithDetailsAsync(id);
        
        if (user == null)
        {
            _logger.LogWarning("Пользователь {UserId} не найден", id);
            return null;
        }

        var completedTasksCount = user.AssignedTasks.Count(t => t.Status == TaskStatus.Done);

        return new UserDetailsDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            IsActive = user.IsActive,
            OwnedProjectsCount = user.OwnedProjects.Count,
            AssignedTasksCount = user.AssignedTasks.Count,
            CompletedTasksCount = completedTasksCount,
            OwnedProjects = user.OwnedProjects.Select(ProjectMapper.ToDto).ToList(),
            AssignedTasks = user.AssignedTasks.Select(TaskMapper.ToDto).ToList()
        };
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        _logger.LogInformation("Получение пользователя по email: {Email}", email);
        var user = await _userRepository.GetByEmailAsync(email);
        
        if (user == null)
        {
            _logger.LogWarning("Пользователь с email {Email} не найден", email);
            return null;
        }

        return UserMapper.ToDto(user);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
    {
        _logger.LogInformation("Создание пользователя: {Email}", dto.Email);

        // Проверяем уникальность email
        var emailExists = await _userRepository.EmailExistsAsync(dto.Email);
        if (emailExists)
        {
            _logger.LogWarning("Попытка создать пользователя с существующим email: {Email}", dto.Email);
            throw new ArgumentException($"Пользователь с email {dto.Email} уже существует");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            AvatarUrl = dto.AvatarUrl,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        await _userRepository.CreateAsync(user);
        _logger.LogInformation("Создан пользователь {UserId}: {Email}", user.Id, user.Email);
        
        return UserMapper.ToDto(user);
    }

    public async Task<bool> UpdateUserAsync(Guid id, UpdateUserDto dto)
    {
        _logger.LogInformation("Обновление пользователя {UserId}", id);
        
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("Попытка обновить несуществующего пользователя {UserId}", id);
            return false;
        }

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.AvatarUrl = dto.AvatarUrl;
        user.IsActive = dto.IsActive;

        await _userRepository.UpdateAsync(user);
        _logger.LogInformation("Пользователь {UserId} обновлён", id);
        return true;
    }

    public async Task<bool> DeleteUserAsync(Guid id)
    {
        _logger.LogInformation("Удаление пользователя {UserId}", id);
        var result = await _userRepository.DeleteAsync(id);
        
        if (result)
            _logger.LogInformation("Пользователь {UserId} удалён", id);
        else
            _logger.LogWarning("Попытка удалить несуществующего пользователя {UserId}", id);
        
        return result;
    }
}