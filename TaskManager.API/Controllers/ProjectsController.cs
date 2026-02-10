using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.DTOs;
using TaskManager.Application.Interfaces;

namespace TaskManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]  // ← Защищён
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetAll()
    {
        var projects = await _projectService.GetAllProjectsAsync();
        return Ok(projects);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProjectDto>> GetById(Guid id)
    {
        var project = await _projectService.GetProjectByIdAsync(id);
        if (project == null)
            return NotFound(new { message = "Проект не найден" });

        return Ok(project);
    }

    [HttpGet("{id}/details")]
    public async Task<ActionResult<ProjectDetailsDto>> GetDetails(Guid id)
    {
        var project = await _projectService.GetProjectDetailsAsync(id);
        if (project == null)
            return NotFound(new { message = "Проект не найден" });

        return Ok(project);
    }

    [HttpGet("owner/{ownerId}")]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetByOwner(Guid ownerId)
    {
        var projects = await _projectService.GetProjectsByOwnerAsync(ownerId);
        return Ok(projects);
    }

    [HttpPost]
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

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateProjectDto dto)
    {
        var success = await _projectService.UpdateProjectAsync(id, dto);
        if (!success)
            return NotFound(new { message = "Проект не найден" });

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]  // ← Только админы
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _projectService.DeleteProjectAsync(id);
        if (!success)
            return NotFound(new { message = "Проект не найден" });

        return NoContent();
    }
}