using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TaskManager.Application.DTOs;
using TaskManager.Application.Interfaces;
using Xunit;
using TaskManager.API.Controllers;

namespace TaskManager.Tests.Controllers;

public class ProjectsControllerTests
{
    private readonly Mock<IProjectService> _projectServiceMock;
    private readonly ProjectsController _controller;

    public ProjectsControllerTests()
    {
        _projectServiceMock = new Mock<IProjectService>();
        _controller = new ProjectsController(_projectServiceMock.Object);
    }

    // ─── GetAll ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithProjects()
    {
        // Arrange
        var projects = new List<ProjectDto>
        {
            BuildProjectDto(name: "Alpha"),
            BuildProjectDto(name: "Beta")
        };
        _projectServiceMock.Setup(s => s.GetAllProjectsAsync()).ReturnsAsync(projects);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(projects);
    }

    [Fact]
    public async Task GetAll_EmptyList_ReturnsOkWithEmptyCollection()
    {
        // Arrange
        _projectServiceMock.Setup(s => s.GetAllProjectsAsync())
            .ReturnsAsync(new List<ProjectDto>());

        // Act
        var result = await _controller.GetAll();

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<ProjectDto>>()
            .Which.Should().BeEmpty();
    }

    // ─── GetById ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingId_Returns200WithProject()
    {
        // Arrange
        var project = BuildProjectDto();
        _projectServiceMock.Setup(s => s.GetProjectByIdAsync(project.Id)).ReturnsAsync(project);

        // Act
        var result = await _controller.GetById(project.Id);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(project);
    }

    [Fact]
    public async Task GetById_NonExistingId_Returns404()
    {
        // Arrange
        _projectServiceMock.Setup(s => s.GetProjectByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((ProjectDto?)null);

        // Act
        var result = await _controller.GetById(Guid.NewGuid());

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ─── GetDetails ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDetails_ExistingId_Returns200WithDetails()
    {
        // Arrange
        var id = Guid.NewGuid();
        var details = new ProjectDetailsDto { Id = id, Name = "Alpha" };
        _projectServiceMock.Setup(s => s.GetProjectDetailsAsync(id)).ReturnsAsync(details);

        // Act
        var result = await _controller.GetDetails(id);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(details);
    }

    [Fact]
    public async Task GetDetails_NotFound_Returns404()
    {
        // Arrange
        _projectServiceMock.Setup(s => s.GetProjectDetailsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((ProjectDetailsDto?)null);

        // Act
        var result = await _controller.GetDetails(Guid.NewGuid());

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ─── GetByOwner ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByOwner_ReturnsProjectsForOwner()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var projects = new List<ProjectDto> { BuildProjectDto() };
        _projectServiceMock.Setup(s => s.GetProjectsByOwnerAsync(ownerId)).ReturnsAsync(projects);

        // Act
        var result = await _controller.GetByOwner(ownerId);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(projects);
    }

    [Fact]
    public async Task GetByOwner_NoProjects_ReturnsEmptyList()
    {
        // Arrange
        _projectServiceMock.Setup(s => s.GetProjectsByOwnerAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<ProjectDto>());

        // Act
        var result = await _controller.GetByOwner(Guid.NewGuid());

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<ProjectDto>>()
            .Which.Should().BeEmpty();
    }

    // ─── Create ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidDto_Returns201Created()
    {
        // Arrange
        var dto = new CreateProjectDto { Name = "New Project" };
        var created = BuildProjectDto(name: "New Project");
        _projectServiceMock.Setup(s => s.CreateProjectAsync(dto)).ReturnsAsync(created);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var createdAt = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdAt.StatusCode.Should().Be(201);
        createdAt.Value.Should().Be(created);
        createdAt.ActionName.Should().Be(nameof(ProjectsController.GetById));
    }

    [Fact]
    public async Task Create_ServiceThrowsArgumentException_Returns400()
    {
        // Arrange
        var dto = new CreateProjectDto { Name = "" };
        _projectServiceMock.Setup(s => s.CreateProjectAsync(dto))
            .ThrowsAsync(new ArgumentException("Название не может быть пустым"));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ─── Update ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ExistingProject_Returns204NoContent()
    {
        // Arrange
        var id = Guid.NewGuid();
        _projectServiceMock.Setup(s => s.UpdateProjectAsync(id, It.IsAny<UpdateProjectDto>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Update(id, new UpdateProjectDto());

        // Assert
        result.Should().BeOfType<NoContentResult>()
            .Which.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task Update_ProjectNotFound_Returns404()
    {
        // Arrange
        _projectServiceMock.Setup(s => s.UpdateProjectAsync(It.IsAny<Guid>(), It.IsAny<UpdateProjectDto>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Update(Guid.NewGuid(), new UpdateProjectDto());

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ─── Delete ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingProject_Returns204NoContent()
    {
        // Arrange
        var id = Guid.NewGuid();
        _projectServiceMock.Setup(s => s.DeleteProjectAsync(id)).ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(id);

        // Assert
        result.Should().BeOfType<NoContentResult>()
            .Which.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task Delete_ProjectNotFound_Returns404()
    {
        // Arrange
        _projectServiceMock.Setup(s => s.DeleteProjectAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(Guid.NewGuid());

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────

    private static ProjectDto BuildProjectDto(string name = "Test Project") => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Description = "Description",
        CreatedAt = DateTime.UtcNow,
        Owner = new UserDto { Id = Guid.NewGuid(), Email = "owner@example.com" },
        TasksCount = 0,
        MembersCount = 1
    };
}