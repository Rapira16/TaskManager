using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TaskManager.Application.DTOs;
using TaskManager.Application.DTOs.Auth;
using TaskManager.Application.Interfaces;
using Xunit;
using TaskManager.API.Controllers;

namespace TaskManager.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _controller = new AuthController(_authServiceMock.Object);
    }

    // ─── Register ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_ValidDto_Returns200WithTokens()
    {
        // Arrange
        var dto = new RegisterDto { Email = "user@example.com", Password = "Secret123!" };
        var expected = BuildAuthResponse();
        _authServiceMock.Setup(s => s.RegisterAsync(dto)).ReturnsAsync(expected);

        // Act
        var result = await _controller.Register(dto);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(expected);
    }

    [Fact]
    public async Task Register_EmailAlreadyTaken_Returns400()
    {
        // Arrange
        var dto = new RegisterDto { Email = "taken@example.com", Password = "Secret123!" };
        _authServiceMock.Setup(s => s.RegisterAsync(dto))
            .ThrowsAsync(new ArgumentException("Email уже используется"));

        // Act
        var result = await _controller.Register(dto);

        // Assert
        var bad = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.StatusCode.Should().Be(400);
    }

    // ─── Login ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithTokens()
    {
        // Arrange
        var dto = new LoginDto { Email = "user@example.com", Password = "Secret123!" };
        var expected = BuildAuthResponse();
        _authServiceMock.Setup(s => s.LoginAsync(dto)).ReturnsAsync(expected);

        // Act
        var result = await _controller.Login(dto);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(expected);
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        // Arrange
        var dto = new LoginDto { Email = "user@example.com", Password = "wrongpass" };
        _authServiceMock.Setup(s => s.LoginAsync(dto))
            .ThrowsAsync(new UnauthorizedAccessException("Неверный пароль"));

        // Act
        var result = await _controller.Login(dto);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>()
            .Which.StatusCode.Should().Be(401);
    }

    // ─── RefreshToken ────────────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshToken_ValidToken_Returns200WithNewTokens()
    {
        // Arrange
        var dto = new RefreshTokenDto { RefreshToken = "valid-refresh-token" };
        var expected = BuildAuthResponse();
        _authServiceMock.Setup(s => s.RefreshTokenAsync(dto.RefreshToken)).ReturnsAsync(expected);

        // Act
        var result = await _controller.RefreshToken(dto);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(expected);
    }

    [Fact]
    public async Task RefreshToken_ExpiredOrInvalidToken_Returns401()
    {
        // Arrange
        var dto = new RefreshTokenDto { RefreshToken = "expired-token" };
        _authServiceMock.Setup(s => s.RefreshTokenAsync(dto.RefreshToken))
            .ThrowsAsync(new UnauthorizedAccessException("Токен истёк"));

        // Act
        var result = await _controller.RefreshToken(dto);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    // ─── Logout ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_AuthenticatedUser_Returns200()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUserContext(userId);
        _authServiceMock.Setup(s => s.RevokeTokenAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _controller.Logout();

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Logout_MissingUserIdClaim_Returns401()
    {
        // Arrange — пустой контекст без claim
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        // Act
        var result = await _controller.Logout();

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Logout_UserNotFound_Returns404()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUserContext(userId);
        _authServiceMock.Setup(s => s.RevokeTokenAsync(userId)).ReturnsAsync(false);

        // Act
        var result = await _controller.Logout();

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ─── GetCurrentUser ───────────────────────────────────────────────────────────

    [Fact]
    public void GetCurrentUser_WithClaims_Returns200WithUserInfo()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUserContext(userId, email: "user@example.com", name: "Иван Петров", role: "User");

        // Act
        var result = _controller.GetCurrentUser();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().NotBeNull();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────

    private static AuthResponseDto BuildAuthResponse() => new()
    {
        AccessToken = "access.token.here",
        RefreshToken = "refresh-token",
        ExpiresAt = DateTime.UtcNow.AddHours(1),
        User = new UserDto { Id = Guid.NewGuid(), Email = "user@example.com", FirstName = "Иван", IsActive = true }
    };

    private void SetupUserContext(Guid userId, string email = "u@u.com", string name = "User Name", string role = "User")
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.Role, role)
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"))
            }
        };
    }
}