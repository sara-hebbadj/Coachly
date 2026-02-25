using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Coachly.Api.Data;
using Coachly.Api.Entities;
using Coachly.Shared.DTOs;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace Coachly.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private static readonly TimeSpan OAuthStateLifetime = TimeSpan.FromMinutes(10);

    private readonly CoachlyDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDataProtector _stateProtector;

    public AuthController(
        CoachlyDbContext db,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IDataProtectionProvider dataProtectionProvider)
    {
        _db = db;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _stateProtector = dataProtectionProvider.CreateProtector("coachly.oauth.google.state.v1");
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
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
        if (user is null || user.PasswordHash != Hash(request.Password))
        {
            return Unauthorized("Invalid email or password.");
        }

        return Ok(new LoginResponseDto
        {
            UserId = user.Id,
            Role = user.Role,
            Token = BuildAppToken(user)
        });
    }

    [HttpGet("external/google/start")]
    public IActionResult GoogleStart([FromQuery] string? mobileCallback)
    {
        var clientId = _configuration["ExternalAuth:Google:ClientId"];
        var callbackPath = _configuration["ExternalAuth:Google:CallbackPath"] ?? "/api/auth/external/google/callback";

        if (string.IsNullOrWhiteSpace(clientId))
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                "Google OAuth is not configured on the API (missing ExternalAuth:Google:ClientId).");
        }

        var mobileCallbackUrl = string.IsNullOrWhiteSpace(mobileCallback)
            ? "coachly://auth-callback"
            : mobileCallback;

        if (!IsAllowedMobileCallback(mobileCallbackUrl))
        {
            return BadRequest("Invalid mobile callback URL.");
        }

        var redirectUri = BuildAbsoluteUrl(callbackPath);
        var protectedState = _stateProtector.Protect(JsonSerializer.Serialize(new OAuthStatePayload
        {
            MobileCallback = mobileCallbackUrl,
            IssuedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16))
        }));

        var query = new Dictionary<string, string?>
        {
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["scope"] = "openid email profile",
            ["state"] = protectedState,
            ["access_type"] = "online",
            ["prompt"] = "select_account"
        };

        var authorizeUrl = QueryHelpers.AddQueryString("https://accounts.google.com/o/oauth2/v2/auth", query);
        return Redirect(authorizeUrl);
    }

    [HttpGet("external/google/callback")]
    public async Task<IActionResult> GoogleCallback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error)
    {
        var mobileCallback = TryResolveMobileCallback(state) ?? "coachly://auth-callback";

        if (!string.IsNullOrWhiteSpace(error))
        {
            return RedirectWithError(mobileCallback, $"Google auth error: {error}");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return RedirectWithError(mobileCallback, "Missing OAuth code.");
        }

        if (string.IsNullOrWhiteSpace(state))
        {
            return RedirectWithError(mobileCallback, "Missing OAuth state.");
        }

        var clientId = _configuration["ExternalAuth:Google:ClientId"];
        var clientSecret = _configuration["ExternalAuth:Google:ClientSecret"];
        var callbackPath = _configuration["ExternalAuth:Google:CallbackPath"] ?? "/api/auth/external/google/callback";

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return RedirectWithError(mobileCallback, "Google OAuth is not configured on API (missing client ID/secret).");
        }

        var redirectUri = BuildAbsoluteUrl(callbackPath);

        var http = _httpClientFactory.CreateClient();
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code"
        });

        var tokenResponse = await http.PostAsync("https://oauth2.googleapis.com/token", tokenRequest);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            return RedirectWithError(mobileCallback, "Google token exchange failed.");
        }

        var tokenPayload = await tokenResponse.Content.ReadFromJsonAsync<GoogleTokenResponse>();
        if (tokenPayload is null || string.IsNullOrWhiteSpace(tokenPayload.AccessToken))
        {
            return RedirectWithError(mobileCallback, "Google token payload is invalid.");
        }

        var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/userinfo");
        userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenPayload.AccessToken);

        var userInfoResponse = await http.SendAsync(userInfoRequest);
        if (!userInfoResponse.IsSuccessStatusCode)
        {
            return RedirectWithError(mobileCallback, "Failed to fetch Google user profile.");
        }

        var googleUser = await userInfoResponse.Content.ReadFromJsonAsync<GoogleUserInfoResponse>();
        if (googleUser is null || string.IsNullOrWhiteSpace(googleUser.Email))
        {
            return RedirectWithError(mobileCallback, "Google profile did not include an email.");
        }

        if (!googleUser.EmailVerified)
        {
            return RedirectWithError(mobileCallback, "Google account email is not verified.");
        }

        var normalizedEmail = googleUser.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        if (user is null)
        {
            user = new User
            {
                FullName = string.IsNullOrWhiteSpace(googleUser.Name) ? googleUser.Email : googleUser.Name,
                Email = normalizedEmail,
                PasswordHash = Hash(Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)),
                Role = "Client",
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        var appToken = BuildAppToken(user);

        var successUrl = QueryHelpers.AddQueryString(mobileCallback, new Dictionary<string, string?>
        {
            ["token"] = appToken,
            ["role"] = user.Role,
            ["userId"] = user.Id.ToString(CultureInfo.InvariantCulture),
            ["provider"] = "google"
        });

        return Redirect(successUrl);
    }

    private IActionResult RedirectWithError(string mobileCallback, string message)
    {
        var url = QueryHelpers.AddQueryString(mobileCallback, new Dictionary<string, string?>
        {
            ["error"] = message
        });

        return Redirect(url);
    }

    private string? TryResolveMobileCallback(string? protectedState)
    {
        if (string.IsNullOrWhiteSpace(protectedState))
        {
            return null;
        }

        try
        {
            var state = _stateProtector.Unprotect(protectedState);
            var payload = JsonSerializer.Deserialize<OAuthStatePayload>(state);
            if (payload is null || string.IsNullOrWhiteSpace(payload.MobileCallback) || string.IsNullOrWhiteSpace(payload.Nonce))
            {
                return null;
            }

            var issuedAt = DateTimeOffset.FromUnixTimeSeconds(payload.IssuedAtUnix);
            if (DateTimeOffset.UtcNow - issuedAt > OAuthStateLifetime)
            {
                return null;
            }

            return IsAllowedMobileCallback(payload.MobileCallback)
                ? payload.MobileCallback
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsAllowedMobileCallback(string callback)
        => callback.StartsWith("coachly://auth-callback", StringComparison.OrdinalIgnoreCase);

    private string BuildAbsoluteUrl(string path)
    {
        if (Uri.TryCreate(path, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.ToString();
        }

        var normalizedPath = path.StartsWith('/') ? path : $"/{path}";
        return $"{Request.Scheme}://{Request.Host}{normalizedPath}";
    }

    private static string BuildAppToken(User user)
        => $"dev-token-{user.Id}-{user.Role.ToLowerInvariant()}";

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    private sealed class GoogleTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
    }

    private sealed class GoogleUserInfoResponse
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("email_verified")]
        public bool EmailVerified { get; set; }
    }

    private sealed class OAuthStatePayload
    {
        public string MobileCallback { get; set; } = string.Empty;
        public long IssuedAtUnix { get; set; }
        public string Nonce { get; set; } = string.Empty;
    }
}
