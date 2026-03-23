using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using TaskManager.Application.DTOs.Auth;
using TaskManager.Application.Interfaces;

namespace TaskManager.API.Controllers;

/// <summary>
/// Контроллер аутентификации и авторизации пользователей.
/// Обеспечивает регистрацию, вход, обновление токенов и выход из системы.
/// </summary>
/// <remarks>
/// Все эндпоинты регистрации, входа и обновления токена защищены
/// ограничением частоты запросов (Rate Limiting) — не более 5 запросов в минуту.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="AuthController"/>.
    /// </summary>
    /// <param name="authService">Сервис аутентификации.</param>
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Регистрирует нового пользователя в системе.
    /// </summary>
    /// <param name="dto">Данные для регистрации: имя, email и пароль.</param>
    /// <returns>
    /// Объект <see cref="AuthResponseDto"/> с JWT access-токеном и refresh-токеном.
    /// </returns>
    /// <response code="200">Пользователь успешно зарегистрирован.</response>
    /// <response code="400">Некорректные данные (например, email уже занят).</response>
    /// <response code="429">Превышен лимит запросов (5 в минуту).</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        try
        {
            var response = await _authService.RegisterAsync(dto);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Выполняет вход пользователя по email и паролю.
    /// </summary>
    /// <param name="dto">Учётные данные пользователя: email и пароль.</param>
    /// <returns>
    /// Объект <see cref="AuthResponseDto"/> с JWT access-токеном и refresh-токеном.
    /// </returns>
    /// <response code="200">Вход выполнен успешно.</response>
    /// <response code="401">Неверный email или пароль.</response>
    /// <response code="429">Превышен лимит попыток входа (5 в минуту).</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        try
        {
            var response = await _authService.LoginAsync(dto);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Обновляет пару токенов по действующему refresh-токену.
    /// </summary>
    /// <param name="dto">DTO с refresh-токеном для обновления.</param>
    /// <returns>
    /// Новый объект <see cref="AuthResponseDto"/> с обновлёнными токенами.
    /// </returns>
    /// <remarks>
    /// После успешного обновления старый refresh-токен аннулируется.
    /// Используйте этот эндпоинт, когда истёк срок действия access-токена.
    /// </remarks>
    /// <response code="200">Токены успешно обновлены.</response>
    /// <response code="401">Refresh-токен недействителен или истёк.</response>
    /// <response code="429">Превышен лимит запросов (5 в минуту).</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken(RefreshTokenDto dto)
    {
        try
        {
            var response = await _authService.RefreshTokenAsync(dto.RefreshToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Завершает сессию текущего пользователя, аннулируя его refresh-токен.
    /// </summary>
    /// <returns>Сообщение об успешном выходе из системы.</returns>
    /// <remarks>
    /// Требует валидного JWT access-токена в заголовке <c>Authorization: Bearer</c>.
    /// Идентификатор пользователя извлекается из клейма <see cref="ClaimTypes.NameIdentifier"/>.
    /// </remarks>
    /// <response code="200">Выход выполнен успешно.</response>
    /// <response code="401">Токен отсутствует, недействителен или не содержит ID пользователя.</response>
    /// <response code="404">Пользователь с данным ID не найден.</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Logout()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Неверный токен" });
        }

        var success = await _authService.RevokeTokenAsync(userId);
        if (!success)
        {
            return NotFound(new { message = "Пользователь не найден" });
        }

        return Ok(new { message = "Вы успешно вышли из системы" });
    }

    /// <summary>
    /// Возвращает информацию о текущем аутентифицированном пользователе.
    /// </summary>
    /// <returns>
    /// Объект с полями <c>userId</c>, <c>email</c>, <c>name</c>, <c>role</c>
    /// и полным списком JWT-клеймов пользователя.
    /// </returns>
    /// <remarks>
    /// Данные извлекаются непосредственно из JWT-токена, без обращения к базе данных.
    /// Требует валидного токена в заголовке <c>Authorization: Bearer</c>.
    /// </remarks>
    /// <response code="200">Информация о пользователе успешно получена.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var name = User.FindFirst(ClaimTypes.Name)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        return Ok(new
        {
            userId,
            email,
            name,
            role,
            claims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }
}