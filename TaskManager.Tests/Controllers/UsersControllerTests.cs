using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TaskManager.Application.DTOs;
using TaskManager.Application.Interfaces;
using Xunit;
using TaskManager.API.Controllers;

namespace TaskManager.Tests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _controller = new UsersController(_userServiceMock.Object);
    }

    // ─── GetAll ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsAllUsers()
    {
        // Arrange
        var users = new List<UserDto> { BuildUserDto(), BuildUserDto() };
        _userServiceMock.Setup(s => s.GetAllUsersAsync()).ReturnsAsync(users);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(users);
    }

    [Fact]
    public async Task GetAll_EmptyList_ReturnsOkWithEmptyCollection()
    {
        _userServiceMock.Setup(s => s.GetAllUsersAsync()).ReturnsAsync(new List<UserDto>());

        var result = await _controller.GetAll();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<UserDto>>()
            .Which.Should().BeEmpty();
    }

    // ─── GetById ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingUser_Returns200()
    {
        // Arrange
        var user = BuildUserDto();
        _userServiceMock.Setup(s => s.GetUserByIdAsync(user.Id)).ReturnsAsync(user);

        // Act
        var result = await _controller.GetById(user.Id);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(user);
    }

    [Fact]
    public async Task GetById_UserNotFound_Returns404()
    {
        _userServiceMock.Setup(s => s.GetUserByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((UserDto?)null);

        var result = await _controller.GetById(Guid.NewGuid());

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ─── GetDetails ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDetails_ExistingUser_Returns200WithDetails()
    {
        // Arrange
        var id = Guid.NewGuid();
        var details = new UserDetailsDto { Id = id, Email = "user@example.com" };
        _userServiceMock.Setup(s => s.GetUserDetailsAsync(id)).ReturnsAsync(details);

        // Act
        var result = await _controller.GetDetails(id);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(details);
    }

    [Fact]
    public async Task GetDetails_UserNotFound_Returns404()
    {
        _userServiceMock.Setup(s => s.GetUserDetailsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((UserDetailsDto?)null);

        var result = await _controller.GetDetails(Guid.NewGuid());

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ─── GetByEmail ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByEmail_ExistingEmail_Returns200()
    {
        // Arrange
        const string email = "user@example.com";
        var user = BuildUserDto(email: email);
        _userServiceMock.Setup(s => s.GetUserByEmailAsync(email)).ReturnsAsync(user);

        // Act
        var result = await _controller.GetByEmail(email);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(user);
    }

    [Fact]
    public async Task GetByEmail_UnknownEmail_Returns404()
    {
        _userServiceMock.Setup(s => s.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((UserDto?)null);

        var result = await _controller.GetByEmail("unknown@example.com");

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ─── Update ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ExistingUser_Returns204()
    {
        var id = Guid.NewGuid();
        _userServiceMock.Setup(s => s.UpdateUserAsync(id, It.IsAny<UpdateUserDto>())).ReturnsAsync(true);

        var result = await _controller.Update(id, new UpdateUserDto());

        result.Should().BeOfType<NoContentResult>().Which.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task Update_UserNotFound_Returns404()
    {
        _userServiceMock.Setup(s => s.UpdateUserAsync(It.IsAny<Guid>(), It.IsAny<UpdateUserDto>()))
            .ReturnsAsync(false);

        var result = await _controller.Update(Guid.NewGuid(), new UpdateUserDto());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ─── Delete ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingUser_Returns204()
    {
        var id = Guid.NewGuid();
        _userServiceMock.Setup(s => s.DeleteUserAsync(id)).ReturnsAsync(true);

        var result = await _controller.Delete(id);

        result.Should().BeOfType<NoContentResult>().Which.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task Delete_UserNotFound_Returns404()
    {
        _userServiceMock.Setup(s => s.DeleteUserAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        var result = await _controller.Delete(Guid.NewGuid());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────

    private static UserDto BuildUserDto(string email = "user@example.com") => new()
    {
        Id = Guid.NewGuid(),
        Email = email,
        FirstName = "Иван",
        LastName = "Петров",
        FullName = "Иван Петров",
        IsActive = true
    };
}