using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TaskManager.Application.Common;
using TaskManager.Application.DTOs;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Entities;
using Xunit;
using TaskManager.API.Controllers;

namespace TaskManager.Tests.Controllers;

public class TasksControllerTests
{
    private readonly Mock<ITaskService> _taskServiceMock;
    private readonly TasksController _controller;

    public TasksControllerTests()
    {
        _taskServiceMock = new Mock<ITaskService>();
        _controller = new TasksController(_taskServiceMock.Object);
    }

    // ─── GetAll ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsList()
    {
        // Arrange
        var tasks = new List<TaskItemDto> { BuildTaskDto(), BuildTaskDto() };
        _taskServiceMock.Setup(s => s.GetAllTasksAsync()).ReturnsAsync(tasks);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(tasks);
    }

    [Fact]
    public async Task GetAll_EmptyList_ReturnsOkWithEmptyCollection()
    {
        _taskServiceMock.Setup(s => s.GetAllTasksAsync()).ReturnsAsync(new List<TaskItemDto>());

        var result = await _controller.GetAll();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<TaskItemDto>>()
            .Which.Should().BeEmpty();
    }

    // ─── GetPaged ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPaged_ValidParameters_ReturnsPagedResult()
    {
        // Arrange
        var parameters = new TaskQueryParameters { PageNumber = 1, PageSize = 10 };
        var paged = new PagedResult<TaskItemDto>
        {
            Items = new List<TaskItemDto> { BuildTaskDto() },
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 1
        };
        _taskServiceMock.Setup(s => s.GetPagedTaskWithFilterAsync(parameters)).ReturnsAsync(paged);

        // Act
        var result = await _controller.GetPaged(parameters);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(paged);
    }

    [Fact]
    public async Task GetPaged_WithStatusFilter_PassesParametersToService()
    {
        // Arrange
        var parameters = new TaskQueryParameters { Status = 1 };
        var paged = new PagedResult<TaskItemDto> { Items = [], PageNumber = 1, PageSize = 10, TotalCount = 0 };
        _taskServiceMock.Setup(s => s.GetPagedTaskWithFilterAsync(
            It.Is<TaskQueryParameters>(p => p.Status == 1)))
            .ReturnsAsync(paged);

        // Act
        var result = await _controller.GetPaged(parameters);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        _taskServiceMock.Verify(s => s.GetPagedTaskWithFilterAsync(
            It.Is<TaskQueryParameters>(p => p.Status == 1)), Times.Once);
    }

    // ─── GetById ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingTask_Returns200()
    {
        // Arrange
        var task = BuildTaskDto();
        _taskServiceMock.Setup(s => s.GetTaskByIdAsync(task.Id)).ReturnsAsync(task);

        // Act
        var result = await _controller.GetById(task.Id);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(task);
    }

    [Fact]
    public async Task GetById_NonExistingTask_Returns404()
    {
        _taskServiceMock.Setup(s => s.GetTaskByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((TaskItemDto?)null);

        var result = await _controller.GetById(Guid.NewGuid());

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ─── Create ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidDto_Returns201Created()
    {
        // Arrange
        var dto = new CreateTaskDto { Title = "New Task", ProjectId = Guid.NewGuid() };
        var created = BuildTaskDto(title: "New Task");
        _taskServiceMock.Setup(s => s.CreateTaskAsync(dto)).ReturnsAsync(created);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var createdAt = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdAt.StatusCode.Should().Be(201);
        createdAt.Value.Should().Be(created);
        createdAt.ActionName.Should().Be(nameof(TasksController.GetById));
    }

    // ─── Update ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ExistingTask_Returns204()
    {
        // Arrange
        var id = Guid.NewGuid();
        _taskServiceMock.Setup(s => s.UpdateTaskAsync(id, It.IsAny<UpdateTaskDto>())).ReturnsAsync(true);

        // Act
        var result = await _controller.Update(id, new UpdateTaskDto());

        // Assert
        result.Should().BeOfType<NoContentResult>().Which.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task Update_TaskNotFound_Returns404()
    {
        _taskServiceMock.Setup(s => s.UpdateTaskAsync(It.IsAny<Guid>(), It.IsAny<UpdateTaskDto>()))
            .ReturnsAsync(false);

        var result = await _controller.Update(Guid.NewGuid(), new UpdateTaskDto());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ─── Delete ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingTask_Returns204()
    {
        var id = Guid.NewGuid();
        _taskServiceMock.Setup(s => s.DeleteTaskAsync(id)).ReturnsAsync(true);

        var result = await _controller.Delete(id);

        result.Should().BeOfType<NoContentResult>().Which.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task Delete_TaskNotFound_Returns404()
    {
        _taskServiceMock.Setup(s => s.DeleteTaskAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        var result = await _controller.Delete(Guid.NewGuid());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ─── GetByStatus ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)] // ToDo
    [InlineData(1)] // InProgress
    [InlineData(2)] // Done
    public async Task GetByStatus_ValidStatus_ReturnsOk(int status)
    {
        // Arrange
        var tasks = new List<TaskItemDto> { BuildTaskDto() };
        _taskServiceMock.Setup(s => s.GetTasksByStatusAsync(status)).ReturnsAsync(tasks);

        // Act
        var result = await _controller.GetByStatus(status);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByStatus_NoTasksFound_ReturnsEmptyCollection()
    {
        _taskServiceMock.Setup(s => s.GetTasksByStatusAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<TaskItemDto>());

        var result = await _controller.GetByStatus(0);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<TaskItemDto>>()
            .Which.Should().BeEmpty();
    }

    // ─── GetByPriority ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)] // Low
    [InlineData(1)] // Medium
    [InlineData(2)] // High
    [InlineData(3)] // Critical
    public async Task GetByPriority_ValidPriority_ReturnsOk(int priority)
    {
        // Arrange
        var tasks = new List<TaskItemDto> { BuildTaskDto() };
        _taskServiceMock.Setup(s => s.GetTasksByPriorityAsync(priority)).ReturnsAsync(tasks);

        // Act
        var result = await _controller.GetByPriority(priority);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(99)]
    [InlineData(4)]
    public async Task GetByPriority_InvalidPriority_Returns400(int invalidPriority)
    {
        // Act
        var result = await _controller.GetByPriority(invalidPriority);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _taskServiceMock.Verify(s => s.GetTasksByPriorityAsync(It.IsAny<int>()), Times.Never);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────

    private static TaskItemDto BuildTaskDto(string title = "Test Task") => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Status = 0,
        StatusName = "ToDo",
        Priority = 1,
        PriorityName = "Medium",
        CreatedAt = DateTime.UtcNow,
        ProjectId = Guid.NewGuid(),
        Project = new ProjectDto { Id = Guid.NewGuid(), Name = "Project" }
    };
}