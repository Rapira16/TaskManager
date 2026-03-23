using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.DTOs;
using TaskManager.Application.Interfaces;

namespace TaskManager.API.Controllers;

/// <summary>
/// Контроллер для управления пользователями системы.
/// Предоставляет операции чтения, обновления и удаления пользователей.
/// </summary>
/// <remarks>
/// Все эндпоинты требуют аутентификации через JWT-токен
/// в заголовке <c>Authorization: Bearer</c>.
/// Получение списка всех пользователей и удаление доступны
/// исключительно пользователям с ролью <c>Admin</c>.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="UsersController"/>.
    /// </summary>
    /// <param name="userService">Сервис для работы с пользователями.</param>
    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Возвращает список всех зарегистрированных пользователей.
    /// </summary>
    /// <returns>Коллекция объектов <see cref="UserDto"/>.</returns>
    /// <remarks>
    /// Доступно только пользователям с ролью <c>Admin</c>.
    /// Для получения данных конкретного пользователя используйте
    /// <see cref="GetById"/> или <see cref="GetByEmail"/>.
    /// </remarks>
    /// <response code="200">Список пользователей успешно получен.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    /// <response code="403">Недостаточно прав — требуется роль <c>Admin</c>.</response>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>
    /// Возвращает пользователя по его уникальному идентификатору.
    /// </summary>
    /// <param name="id">Уникальный идентификатор пользователя.</param>
    /// <returns>Объект <see cref="UserDto"/> с основными данными пользователя.</returns>
    /// <remarks>
    /// Возвращает только базовые поля. Для получения расширенной информации,
    /// включая задачи и проекты пользователя, используйте <see cref="GetDetails"/>.
    /// </remarks>
    /// <response code="200">Пользователь найден и возвращён.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    /// <response code="404">Пользователь с указанным идентификатором не найден.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(Guid id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
            return NotFound(new { message = "Пользователь не найден" });
        return Ok(user);
    }

    /// <summary>
    /// Возвращает детальную информацию о пользователе, включая его задачи и проекты.
    /// </summary>
    /// <param name="id">Уникальный идентификатор пользователя.</param>
    /// <returns>
    /// Объект <see cref="UserDetailsDto"/> с расширенными данными пользователя.
    /// </returns>
    /// <remarks>
    /// В отличие от <see cref="GetById"/>, этот эндпоинт возвращает полную
    /// информацию о пользователе: назначенные задачи, участие в проектах
    /// и прочие связанные данные.
    /// </remarks>
    /// <response code="200">Детальная информация о пользователе получена.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    /// <response code="404">Пользователь с указанным идентификатором не найден.</response>
    [HttpGet("{id}/details")]
    [ProducesResponseType(typeof(UserDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDetailsDto>> GetDetails(Guid id)
    {
        var user = await _userService.GetUserDetailsAsync(id);
        if (user == null)
            return NotFound(new { message = "Пользователь не найден" });
        return Ok(user);
    }

    /// <summary>
    /// Возвращает пользователя по его адресу электронной почты.
    /// </summary>
    /// <param name="email">Адрес электронной почты пользователя.</param>
    /// <returns>Объект <see cref="UserDto"/> с основными данными пользователя.</returns>
    /// <remarks>
    /// Поиск выполняется без учёта регистра. Полезен для поиска пользователя
    /// при отсутствии его идентификатора, например при интеграциях или импорте данных.
    /// </remarks>
    /// <response code="200">Пользователь найден и возвращён.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    /// <response code="404">Пользователь с указанным email не найден.</response>
    [HttpGet("by-email/{email}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetByEmail(string email)
    {
        var user = await _userService.GetUserByEmailAsync(email);
        if (user == null)
            return NotFound(new { message = "Пользователь не найден" });
        return Ok(user);
    }

    /// <summary>
    /// Обновляет данные существующего пользователя.
    /// </summary>
    /// <param name="id">Уникальный идентификатор обновляемого пользователя.</param>
    /// <param name="dto">Новые данные пользователя: имя, email и прочие поля.</param>
    /// <returns>Пустой ответ при успешном обновлении.</returns>
    /// <remarks>
    /// Пользователь может обновлять только собственный профиль.
    /// Для смены пароля используйте соответствующий эндпоинт в
    /// <see cref="AuthController"/>.
    /// </remarks>
    /// <response code="204">Данные пользователя успешно обновлены.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    /// <response code="404">Пользователь с указанным идентификатором не найден.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdateUserDto dto)
    {
        var success = await _userService.UpdateUserAsync(id, dto);
        if (!success)
            return NotFound(new { message = "Пользователь не найден" });
        return NoContent();
    }

    /// <summary>
    /// Удаляет пользователя по его идентификатору.
    /// </summary>
    /// <param name="id">Уникальный идентификатор удаляемого пользователя.</param>
    /// <returns>Пустой ответ при успешном удалении.</returns>
    /// <remarks>
    /// Доступно только пользователям с ролью <c>Admin</c>.
    /// Операция необратима. При удалении пользователя связанные задачи
    /// и членство в проектах обрабатываются согласно бизнес-логике сервиса.
    /// </remarks>
    /// <response code="204">Пользователь успешно удалён.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    /// <response code="403">Недостаточно прав — требуется роль <c>Admin</c>.</response>
    /// <response code="404">Пользователь с указанным идентификатором не найден.</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _userService.DeleteUserAsync(id);
        if (!success)
            return NotFound(new { message = "Пользователь не найден" });
        return NoContent();
    }
}