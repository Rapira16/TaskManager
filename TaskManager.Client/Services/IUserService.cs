using TaskManager.Client.Models;

namespace TaskManager.Client.Services;

public interface IUserService
{
    Task<List<UserDetailsDto>> GetAllAsync();
    Task<UserDetailsDto?> GetDetailsAsync(Guid id);
}