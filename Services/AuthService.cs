using System.Net.Http.Json;
using Coachly.Shared.DTOs;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace Coachly.Services;

public class AuthService(HttpClient httpClient, AuthProviderOptions providerOptions)
{
#pragma warning disable CA1416
    private const string AuthTokenKey = "auth.token";

    public event Action? AuthStateChanged;

    public bool IsAuthenticated { get; private set; }
    public string CurrentRole { get; private set; } = string.Empty;

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
    {
        var normalizedRequest = new LoginRequestDto
        {
            Email = request.Email.Trim(),
            Password = request.Password.Trim()
        };

        var response = await httpClient.PostAsJsonAsync("api/auth/login", normalizedRequest);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var dto = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        if (dto is null || string.IsNullOrWhiteSpace(dto.Token))
        {
            return null;
        }

        await SecureStorage.Default.SetAsync(AuthTokenKey, dto.Token);

        IsAuthenticated = true;
        CurrentRole = dto.Role;
        AuthStateChanged?.Invoke();

        return dto;
    }

    public async Task<(bool IsSuccess, string? Error)> RegisterAsync(string fullName, string email, string password, string role)
    {
        var request = new RegisterRequestDto
        {
            FullName = fullName.Trim(),
            Email = email.Trim(),
            Password = password.Trim(),
            Role = role
        };

        var response = await httpClient.PostAsJsonAsync("api/auth/register", request);
        if (response.IsSuccessStatusCode)
        {
            return (true, null);
        }

        var error = await response.Content.ReadAsStringAsync();
        return (false, string.IsNullOrWhiteSpace(error) ? "Registration failed." : error);
    }

    public async Task<(bool IsSuccess, string Error)> SignInWithGoogleAsync()
    {
        if (!providerOptions.EnableExternalProviderSignIn)
        {
            return (false, "Google sign-in is disabled until backend OAuth endpoints are fully configured.");
        }

        if (!providerOptions.IsGoogleConfigured)
        {
            return (false, "Configure GoogleClientId and BackendOAuthStartUrl in MauiProgram.");
        }

        return await StartExternalSignInAsync("google");
    }

    public async Task<(bool IsSuccess, string Error)> SignInWithAppleAsync()
    {
        if (!providerOptions.EnableExternalProviderSignIn)
        {
            return (false, "Apple sign-in is disabled until backend OAuth endpoints are fully configured.");
        }

        if (!providerOptions.IsAppleConfigured)
        {
            return (false, "Configure AppleClientId and BackendOAuthStartUrl in MauiProgram.");
        }

        return await StartExternalSignInAsync("apple");
    }

    private async Task<(bool IsSuccess, string Error)> StartExternalSignInAsync(string provider)
    {
        var startUrl = BuildExternalStartUrl(provider);

        try
        {
            await Browser.Default.OpenAsync(startUrl, BrowserLaunchMode.SystemPreferred);
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, $"Could not open browser for {provider} sign-in: {ex.Message}");
        }
    }

    public void Logout()
    {
        SecureStorage.Default.Remove(AuthTokenKey);
        IsAuthenticated = false;
        CurrentRole = string.Empty;
        AuthStateChanged?.Invoke();
    }

    private string BuildExternalStartUrl(string provider)
    {
        var baseUrl = providerOptions.BackendOAuthStartUrl.TrimEnd('/');
        return $"{baseUrl}/api/auth/external/{provider}/start";
    }
#pragma warning restore CA1416
}
