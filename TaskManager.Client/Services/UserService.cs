using System.Net.Http.Json;
using TaskManager.Client.Models;

namespace TaskManager.Client.Services;

public class UserService : IUserService
{
    private readonly HttpClient _httpClient;

    public UserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<UserDetailsDto>> GetAllAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<UserDetailsDto>>("api/users");
            return result ?? new List<UserDetailsDto>();
        }
        catch
        {
            return new List<UserDetailsDto>();
        }
    }

    public async Task<UserDetailsDto?> GetDetailsAsync(Guid id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<UserDetailsDto>($"api/users/{id}/details");
        }
        catch
        {
            return null;
        }
    }
}