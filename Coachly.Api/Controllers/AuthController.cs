using System.Security.Cryptography;
using System.Text;
using Coachly.Api.Data;
using Coachly.Api.Entities;
using Coachly.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Coachly.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly CoachlyDbContext _db;

    public AuthController(CoachlyDbContext db)
    {
        _db = db;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthUserDto>> Register([FromBody] RegisterRequestDto request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var role = request.Role.Equals("Coach", StringComparison.OrdinalIgnoreCase) ? "Coach" : "Client";

        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail))
        {
            return Conflict("Email is already in use.");
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            PasswordHash = Hash(request.Password),
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new AuthUserDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.Trim().ToLower());
        if (user is null || user.PasswordHash != Hash(request.Password))
        {
            return Unauthorized("Invalid email or password.");
        }

        return Ok(new LoginResponseDto
        {
            UserId = user.Id,
            Role = user.Role,
            Token = $"dev-token-{user.Id}-{user.Role.ToLowerInvariant()}"
        });
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
