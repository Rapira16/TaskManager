using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.DTOs;
using TaskManager.Application.Interfaces;

namespace TaskManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]  // ← Защищён
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Получить всех пользователей (только администраторы)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]  // ← Только админы
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetById(Guid id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
            return NotFound(new { message = "Пользователь не найден" });

        return Ok(user);
    }

    [HttpGet("{id}/details")]
    public async Task<ActionResult<UserDetailsDto>> GetDetails(Guid id)
    {
        var user = await _userService.GetUserDetailsAsync(id);
        if (user == null)
            return NotFound(new { message = "Пользователь не найден" });

        return Ok(user);
    }

    [HttpGet("by-email/{email}")]
    public async Task<ActionResult<UserDto>> GetByEmail(string email)
    {
        var user = await _userService.GetUserByEmailAsync(email);
        if (user == null)
            return NotFound(new { message = "Пользователь не найден" });

        return Ok(user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateUserDto dto)
    {
        var success = await _userService.UpdateUserAsync(id, dto);
        if (!success)
            return NotFound(new { message = "Пользователь не найден" });

        return NoContent();
    }

    /// <summary>
    /// Удалить пользователя (только администраторы)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]  // ← Только админы
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _userService.DeleteUserAsync(id);
        if (!success)
            return NotFound(new { message = "Пользователь не найден" });

        return NoContent();
    }
}