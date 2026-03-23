using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManager.Application.DTOs;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Common;
using TaskManager.Domain.Entities;

namespace TaskManager.API.Controllers;

/// <summary>
/// Контроллер для управления задачами.
/// Предоставляет операции создания, чтения, обновления и удаления задач,
/// а также фильтрацию по статусу и приоритету.
/// </summary>
/// <remarks>
/// Все эндпоинты требуют аутентификации через JWT-токен
/// в заголовке <c>Authorization: Bearer</c>.
/// Удаление задачи доступно исключительно пользователям с ролью <c>Admin</c>.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="TasksController"/>.
    /// </summary>
    /// <param name="taskService">Сервис для работы с задачами.</param>
    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>
    /// Возвращает полный список всех задач без фильтрации.
    /// </summary>
    /// <returns>Коллекция объектов <see cref="TaskItemDto"/>.</returns>
    /// <remarks>
    /// При большом количестве задач рекомендуется использовать
    /// <see cref="GetPaged"/> с пагинацией и фильтрацией.
    /// </remarks>
    /// <response code="200">Список задач успешно получен.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TaskItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<TaskItemDto>>> GetAll()
    {
        var tasks = await _taskService.GetAllTasksAsync();
        return Ok(tasks);
    }

    /// <summary>
    /// Возвращает задачи с поддержкой пагинации, фильтрации и сортировки.
    /// </summary>
    /// <param name="parameters">
    /// Параметры запроса: номер страницы, размер страницы, поле сортировки,
    /// фильтры по статусу, приоритету и исполнителю.
    /// </param>
    /// <returns>
    /// Объект <see cref="PagedResult{TaskItemDto}"/>, содержащий текущую страницу задач,
    /// общее количество записей и метаданные пагинации.
    /// </returns>
    /// <remarks>
    /// Предпочтительный эндпоинт для отображения задач в UI.
    /// Параметры передаются через строку запроса, например:
    /// <code>GET /api/tasks/paged?page=1&amp;pageSize=20&amp;status=1&amp;sortBy=dueDate</code>
    /// </remarks>
    /// <response code="200">Страница задач успешно получена.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PagedResult<TaskItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<TaskItemDto>>> GetPaged(
        [FromQuery] TaskQueryParameters parameters)
    {
        var result = await _taskService.GetPagedTaskWithFilterAsync(parameters);
        return Ok(result);
    }

    /// <summary>
    /// Возвращает задачу по её уникальному идентификатору.
    /// </summary>
    /// <param name="id">Уникальный идентификатор задачи.</param>
    /// <returns>Объект <see cref="TaskItemDto"/> с данными задачи.</returns>
    /// <response code="200">Задача найдена и возвращена.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    /// <response code="404">Задача с указанным идентификатором не найдена.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskItemDto>> GetById(Guid id)
    {
        var task = await _taskService.GetTaskByIdAsync(id);
        if (task == null)
            return NotFound(new { message = "Задача не найдена" });
        return Ok(task);
    }

    /// <summary>
    /// Создаёт новую задачу.
    /// </summary>
    /// <param name="dto">Данные для создания задачи: название, описание, приоритет, исполнитель.</param>
    /// <returns>Созданный объект <see cref="TaskItemDto"/> с присвоенным идентификатором.</returns>
    /// <remarks>
    /// При успешном создании возвращает код <c>201 Created</c> и заголовок
    /// <c>Location</c> с URL нового ресурса, указывающим на <see cref="GetById"/>.
    /// </remarks>
    /// <response code="201">Задача успешно создана.</response>
    /// <response code="400">Некорректные данные запроса.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TaskItemDto>> Create(CreateTaskDto dto)
    {
        var task = await _taskService.CreateTaskAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }

    /// <summary>
    /// Обновляет данные существующей задачи.
    /// </summary>
    /// <param name="id">Уникальный идентификатор обновляемой задачи.</param>
    /// <param name="dto">Новые данные задачи.</param>
    /// <returns>Пустой ответ при успешном обновлении.</returns>
    /// <response code="204">Задача успешно обновлена.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    /// <response code="404">Задача с указанным идентификатором не найдена.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdateTaskDto dto)
    {
        var success = await _taskService.UpdateTaskAsync(id, dto);
        if (!success)
            return NotFound(new { message = "Задача не найдена" });
        return NoContent();
    }

    /// <summary>
    /// Удаляет задачу по её идентификатору.
    /// </summary>
    /// <param name="id">Уникальный идентификатор удаляемой задачи.</param>
    /// <returns>Пустой ответ при успешном удалении.</returns>
    /// <remarks>
    /// Доступно только пользователям с ролью <c>Admin</c>.
    /// Операция необратима — восстановление удалённой задачи невозможно.
    /// </remarks>
    /// <response code="204">Задача успешно удалена.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    /// <response code="403">Недостаточно прав — требуется роль <c>Admin</c>.</response>
    /// <response code="404">Задача с указанным идентификатором не найдена.</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _taskService.DeleteTaskAsync(id);
        if (!success)
            return NotFound(new { message = "Задача не найдена" });
        return NoContent();
    }

    /// <summary>
    /// Возвращает все задачи с указанным статусом.
    /// </summary>
    /// <param name="status">
    /// Числовое значение статуса задачи.
    /// Соответствует значениям перечисления статусов задачи в домене.
    /// </param>
    /// <returns>
    /// Коллекция объектов <see cref="TaskItemDto"/> с заданным статусом.
    /// Возвращает пустую коллекцию, если задачи не найдены.
    /// </returns>
    /// <response code="200">Список задач по статусу успешно получен.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(IEnumerable<TaskItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<TaskItemDto>>> GetByStatus(int status)
    {
        var tasks = await _taskService.GetTasksByStatusAsync(status);
        return Ok(tasks);
    }

    /// <summary>
    /// Возвращает все задачи с указанным приоритетом.
    /// </summary>
    /// <param name="priority">
    /// Числовое значение приоритета из перечисления <see cref="TaskPriority"/>.
    /// </param>
    /// <returns>
    /// Коллекция объектов <see cref="TaskItemDto"/> с заданным приоритетом.
    /// Возвращает пустую коллекцию, если задачи не найдены.
    /// </returns>
    /// <remarks>
    /// Перед запросом к сервису выполняется валидация значения приоритета
    /// через <see cref="Enum.IsDefined"/>. Некорректные значения отклоняются
    /// с кодом <c>400 Bad Request</c>.
    /// </remarks>
    /// <response code="200">Список задач по приоритету успешно получен.</response>
    /// <response code="400">Передано некорректное значение приоритета.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    [HttpGet("priority/{priority}")]
    [ProducesResponseType(typeof(IEnumerable<TaskItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<TaskItemDto>>> GetByPriority(int priority)
    {
        if (!Enum.IsDefined(typeof(TaskPriority), priority))
        {
            return BadRequest(new { message = "Некорректный приоритет" });
        }
        var tasks = await _taskService.GetTasksByPriorityAsync(priority);
        return Ok(tasks);
    }
}