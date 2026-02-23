using Coachly.Shared.DTOs;

namespace Coachly.Services;

public class AuthService
{
    private readonly List<AppUser> _users =
    [
        new() { Id = 1, FullName = "Demo Client", Email = "client@coachly.app", Password = "password123", Role = "Client" },
        new() { Id = 2, FullName = "Demo Coach", Email = "coach@coachly.app", Password = "password123", Role = "Coach" }
    ];

    public event Action? AuthStateChanged;

    public bool IsAuthenticated { get; private set; }
    public string CurrentRole { get; private set; } = string.Empty;

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
    {
        await Task.Delay(200);

        var user = _users.FirstOrDefault(u =>
            u.Email.Equals(request.Email.Trim(), StringComparison.OrdinalIgnoreCase)
            && u.Password == request.Password);

        if (user is null)
        {
            return null;
        }

        IsAuthenticated = true;
        CurrentRole = user.Role;
        AuthStateChanged?.Invoke();

        return new LoginResponseDto
        {
            UserId = user.Id,
            Role = user.Role,
            Token = $"demo-token-{user.Id}"
        };
    }

    public async Task<(bool IsSuccess, string? Error)> RegisterAsync(string fullName, string email, string password, string role)
    {
        await Task.Delay(200);

        if (_users.Any(u => u.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return (false, "An account with this email already exists.");
        }

        _users.Add(new AppUser
        {
            Id = _users.Max(u => u.Id) + 1,
            FullName = fullName.Trim(),
            Email = email.Trim(),
            Password = password,
            Role = role
        });

        return (true, null);
    }

    public void Logout()
    {
        IsAuthenticated = false;
        CurrentRole = string.Empty;
        AuthStateChanged?.Invoke();
    }

    private sealed class AppUser
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Client";
    }
}
