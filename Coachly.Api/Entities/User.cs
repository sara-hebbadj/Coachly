namespace Coachly.Api.Entities;

public class User
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = "Client"; // Client | Coach

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
