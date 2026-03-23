using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.DTOs;
using TaskManager.Application.Interfaces;

namespace TaskManager.API.Controllers;

/// <summary>
/// Контроллер для управления проектами.
/// Предоставляет CRUD-операции над проектами системы.
/// </summary>
/// <remarks>
/// Все эндпоинты требуют аутентификации через JWT-токен
/// в заголовке <c>Authorization: Bearer</c>.
/// Удаление проекта доступно исключительно пользователям с ролью <c>Admin</c>.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="ProjectsController"/>.
    /// </summary>
    /// <param name="projectService">Сервис для работы с проектами.</param>
    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    /// <summary>
    /// Возвращает список всех проектов.
    /// </summary>
    /// <returns>Коллекция объектов <see cref="ProjectDto"/>.</returns>
    /// <response code="200">Список проектов успешно получен.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetAll()
    {
        var projects = await _projectService.GetAllProjectsAsync();
        return Ok(projects);
    }

    /// <summary>
    /// Возвращает проект по его уникальному идентификатору.
    /// </summary>
    /// <param name="id">Уникальный идентификатор проекта.</param>
    /// <returns>Объект <see cref="ProjectDto"/> с основными данными проекта.</returns>
    /// <response code="200">Проект найден и возвращён.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    /// <response code="404">Проект с указанным идентификатором не найден.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDto>> GetById(Guid id)
    {
        var project = await _projectService.GetProjectByIdAsync(id);
        if (project == null)
            return NotFound(new { message = "Проект не найден" });
        return Ok(project);
    }

    /// <summary>
    /// Возвращает детальную информацию о проекте, включая задачи и участников.
    /// </summary>
    /// <param name="id">Уникальный идентификатор проекта.</param>
    /// <returns>
    /// Объект <see cref="ProjectDetailsDto"/> с расширенными данными проекта.
    /// </returns>
    /// <remarks>
    /// В отличие от <see cref="GetById"/>, этот эндпоинт возвращает полную
    /// информацию: список задач, участников и прочие связанные данные.
    /// </remarks>
    /// <response code="200">Детальная информация о проекте получена.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    /// <response code="404">Проект с указанным идентификатором не найден.</response>
    [HttpGet("{id}/details")]
    [ProducesResponseType(typeof(ProjectDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDetailsDto>> GetDetails(Guid id)
    {
        var project = await _projectService.GetProjectDetailsAsync(id);
        if (project == null)
            return NotFound(new { message = "Проект не найден" });
        return Ok(project);
    }

    /// <summary>
    /// Возвращает все проекты, принадлежащие указанному владельцу.
    /// </summary>
    /// <param name="ownerId">Уникальный идентификатор владельца проектов.</param>
    /// <returns>
    /// Коллекция объектов <see cref="ProjectDto"/>, принадлежащих владельцу.
    /// Возвращает пустую коллекцию, если проекты не найдены.
    /// </returns>
    /// <response code="200">Список проектов владельца успешно получен.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    [HttpGet("owner/{ownerId}")]
    [ProducesResponseType(typeof(IEnumerable<ProjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetByOwner(Guid ownerId)
    {
        var projects = await _projectService.GetProjectsByOwnerAsync(ownerId);
        return Ok(projects);
    }

    /// <summary>
    /// Создаёт новый проект.
    /// </summary>
    /// <param name="dto">Данные для создания проекта: название, описание и прочие поля.</param>
    /// <returns>
    /// Созданный объект <see cref="ProjectDto"/> с присвоенным идентификатором.
    /// </returns>
    /// <remarks>
    /// При успешном создании возвращает код <c>201 Created</c> и заголовок
    /// <c>Location</c> с URL нового ресурса, указывающим на <see cref="GetById"/>.
    /// </remarks>
    /// <response code="201">Проект успешно создан.</response>
    /// <response code="400">Некорректные данные (например, пустое название).</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProjectDto>> Create(CreateProjectDto dto)
    {
        try
        {
            var project = await _projectService.CreateProjectAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Обновляет данные существующего проекта.
    /// </summary>
    /// <param name="id">Уникальный идентификатор обновляемого проекта.</param>
    /// <param name="dto">Новые данные проекта.</param>
    /// <returns>Пустой ответ при успешном обновлении.</returns>
    /// <response code="204">Проект успешно обновлён.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    /// <response code="404">Проект с указанным идентификатором не найден.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdateProjectDto dto)
    {
        var success = await _projectService.UpdateProjectAsync(id, dto);
        if (!success)
            return NotFound(new { message = "Проект не найден" });
        return NoContent();
    }

    /// <summary>
    /// Удаляет проект по его идентификатору.
    /// </summary>
    /// <param name="id">Уникальный идентификатор удаляемого проекта.</param>
    /// <returns>Пустой ответ при успешном удалении.</returns>
    /// <remarks>
    /// Доступно только пользователям с ролью <c>Admin</c>.
    /// Операция необратима — восстановление удалённого проекта невозможно.
    /// </remarks>
    /// <response code="204">Проект успешно удалён.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    /// <response code="403">Недостаточно прав — требуется роль <c>Admin</c>.</response>
    /// <response code="404">Проект с указанным идентификатором не найден.</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _projectService.DeleteProjectAsync(id);
        if (!success)
            return NotFound(new { message = "Проект не найден" });
        return NoContent();
    }
}