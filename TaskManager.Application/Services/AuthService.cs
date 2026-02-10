using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TaskManager.Application.DTOs;
using TaskManager.Application.DTOs.Auth;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Mappings;
using TaskManager.Domain.Entities;
using BCrypt.Net;

namespace TaskManager.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IJwtService jwtService,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        _logger.LogInformation("Регистрация нового пользователя: {Email}", dto.Email);

        // Проверяем уникальность email
        var existingUser = await _userRepository.GetByEmailAsync(dto.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Попытка регистрации с существующим email: {Email}", dto.Email);
            throw new ArgumentException("Пользователь с таким email уже существует");
        }

        // Хэшируем пароль
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PasswordHash = passwordHash,
            Role = UserRole.User,  // По умолчанию обычный пользователь
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Генерируем токены
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(
            Convert.ToDouble(_configuration["Jwt:RefreshTokenExpirationDays"]));

        await _userRepository.CreateAsync(user);

        _logger.LogInformation("Пользователь {UserId} успешно зарегистрирован", user.Id);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(
                Convert.ToDouble(_configuration["Jwt:AccessTokenExpirationMinutes"])),
            User = UserMapper.ToDto(user)
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        _logger.LogInformation("Попытка входа: {Email}", dto.Email);

        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null)
        {
            _logger.LogWarning("Попытка входа с несуществующим email: {Email}", dto.Email);
            throw new UnauthorizedAccessException("Неверный email или пароль");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Попытка входа неактивного пользователя: {Email}", dto.Email);
            throw new UnauthorizedAccessException("Аккаунт деактивирован");
        }

        // Проверяем пароль
        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            _logger.LogWarning("Неверный пароль для пользователя: {Email}", dto.Email);
            throw new UnauthorizedAccessException("Неверный email или пароль");
        }

        // Генерируем новые токены
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(
            Convert.ToDouble(_configuration["Jwt:RefreshTokenExpirationDays"]));
        user.LastLoginAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("Пользователь {UserId} успешно вошёл в систему", user.Id);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(
                Convert.ToDouble(_configuration["Jwt:AccessTokenExpirationMinutes"])),
            User = UserMapper.ToDto(user)
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        _logger.LogInformation("Попытка обновления токена");

        var user = await _userRepository.GetByRefreshTokenAsync(refreshToken);
        if (user == null || user.RefreshToken != refreshToken)
        {
            _logger.LogWarning("Попытка использовать неверный refresh token");
            throw new UnauthorizedAccessException("Неверный refresh token");
        }

        if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            _logger.LogWarning("Попытка использовать истёкший refresh token для пользователя {UserId}", user.Id);
            throw new UnauthorizedAccessException("Refresh token истёк");
        }

        // Генерируем новые токены
        var accessToken = _jwtService.GenerateAccessToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(
            Convert.ToDouble(_configuration["Jwt:RefreshTokenExpirationDays"]));

        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("Токены успешно обновлены для пользователя {UserId}", user.Id);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(
                Convert.ToDouble(_configuration["Jwt:AccessTokenExpirationMinutes"])),
            User = UserMapper.ToDto(user)
        };
    }

    public async Task<bool> RevokeTokenAsync(Guid userId)
    {
        _logger.LogInformation("Отзыв токена для пользователя {UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return false;

        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;

        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("Токен отозван для пользователя {UserId}", userId);
        return true;
    }
}