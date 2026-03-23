using System.Net.Http.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using TaskManager.Client.Auth;
using TaskManager.Client.Models;

namespace TaskManager.Client.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authStateProvider;

    public AuthService(
        HttpClient httpClient,
        ILocalStorageService localStorage,
        AuthenticationStateProvider authStateProvider)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);

            if (!response.IsSuccessStatusCode)
                return null;

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

            if (authResponse != null)
            {
                await _localStorage.SetItemAsStringAsync("accessToken", authResponse.AccessToken);
                await _localStorage.SetItemAsStringAsync("refreshToken", authResponse.RefreshToken);

                await ((CustomAuthStateProvider)_authStateProvider)
                    .MarkUserAsAuthenticated(authResponse.AccessToken);
            }

            return authResponse;
        }
        catch
        {
            return null;
        }
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);

            if (!response.IsSuccessStatusCode)
                return null;

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

            if (authResponse != null)
            {
                await _localStorage.SetItemAsStringAsync("accessToken", authResponse.AccessToken);
                await _localStorage.SetItemAsStringAsync("refreshToken", authResponse.RefreshToken);

                await ((CustomAuthStateProvider)_authStateProvider)
                    .MarkUserAsAuthenticated(authResponse.AccessToken);
            }

            return authResponse;
        }
        catch
        {
            return null;
        }
    }

    public async Task LogoutAsync()
    {
        await _httpClient.PostAsync("api/auth/logout", null);
        await ((CustomAuthStateProvider)_authStateProvider).MarkUserAsLoggedOut();
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<dynamic>("api/auth/me");
            // Преобразуй в UserDto если нужно
            return null; // Реализуй парсинг
        }
        catch
        {
            return null;
        }
    }
}