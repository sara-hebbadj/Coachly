using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
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
        _stateProtector = dataProtectionProvider.CreateProtector("coachly.oauth.state.v1");
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
            Email = normalizedEmail,
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

        return Ok(ToLoginResponse(user));
    }

    [HttpGet("session")]
    public async Task<ActionResult<LoginResponseDto>> GetSession()
    {
        var token = Request.Headers.Authorization
            .FirstOrDefault(h => h.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            ?.Substring("Bearer ".Length)
            .Trim();

        if (string.IsNullOrWhiteSpace(token))
        {
            return Unauthorized("Missing bearer token.");
        }

        var user = await TryResolveTokenUserAsync(token);
        if (user is null)
        {
            return Unauthorized("Invalid token.");
        }

        return Ok(ToLoginResponse(user));
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

        var mobileCallbackUrl = ResolveMobileCallback(mobileCallback);
        if (!IsAllowedMobileCallback(mobileCallbackUrl))
        {
            return BadRequest("Invalid mobile callback URL.");
        }

        var redirectUri = BuildAbsoluteUrl(callbackPath);
        var protectedState = BuildState(mobileCallbackUrl, "google");

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

    [HttpGet("external/apple/start")]
    public IActionResult AppleStart([FromQuery] string? mobileCallback)
    {
        var clientId = _configuration["ExternalAuth:Apple:ClientId"];
        var callbackPath = _configuration["ExternalAuth:Apple:CallbackPath"] ?? "/api/auth/external/apple/callback";

        if (string.IsNullOrWhiteSpace(clientId))
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                "Apple OAuth is not configured on the API (missing ExternalAuth:Apple:ClientId).");
        }

        var mobileCallbackUrl = ResolveMobileCallback(mobileCallback);
        if (!IsAllowedMobileCallback(mobileCallbackUrl))
        {
            return BadRequest("Invalid mobile callback URL.");
        }

        var redirectUri = BuildAbsoluteUrl(callbackPath);
        var protectedState = BuildState(mobileCallbackUrl, "apple");

        var query = new Dictionary<string, string?>
        {
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code id_token",
            ["response_mode"] = "form_post",
            ["scope"] = "name email",
            ["state"] = protectedState,
            ["nonce"] = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant()
        };

        var authorizeUrl = QueryHelpers.AddQueryString("https://appleid.apple.com/auth/authorize", query);
        return Redirect(authorizeUrl);
    }

    [HttpGet("external/google/callback")]
    public async Task<IActionResult> GoogleCallback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error)
    {
        var statePayload = TryResolveState(state);
        var mobileCallback = statePayload?.MobileCallback ?? "coachly://auth-callback";

        if (!string.IsNullOrWhiteSpace(error))
        {
            return RedirectWithError(mobileCallback, $"Google auth error: {error}");
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(statePayload?.MobileCallback))
        {
            return RedirectWithError(mobileCallback, "Missing OAuth state/code.");
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

        var user = await UpsertExternalUserAsync(googleUser.Email, googleUser.Name, "google");

        return RedirectWithSuccess(mobileCallback, user, "google");
    }

    [HttpGet("external/apple/callback")]
    public async Task<IActionResult> AppleCallbackGet([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? id_token, [FromQuery] string? error)
        => await AppleCallbackInternal(code, state, id_token, error);

    [HttpPost("external/apple/callback")]
    public async Task<IActionResult> AppleCallbackPost([FromForm] string? code, [FromForm] string? state, [FromForm] string? id_token, [FromForm] string? error)
        => await AppleCallbackInternal(code, state, id_token, error);

    private async Task<IActionResult> AppleCallbackInternal(string? code, string? state, string? idTokenFromAuth, string? error)
    {
        var statePayload = TryResolveState(state);
        var mobileCallback = statePayload?.MobileCallback ?? "coachly://auth-callback";

        if (!string.IsNullOrWhiteSpace(error))
        {
            return RedirectWithError(mobileCallback, $"Apple auth error: {error}");
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(statePayload?.MobileCallback))
        {
            return RedirectWithError(mobileCallback, "Missing OAuth state/code.");
        }

        var clientId = _configuration["ExternalAuth:Apple:ClientId"];
        var teamId = _configuration["ExternalAuth:Apple:TeamId"];
        var keyId = _configuration["ExternalAuth:Apple:KeyId"];
        var privateKey = _configuration["ExternalAuth:Apple:PrivateKey"];
        var callbackPath = _configuration["ExternalAuth:Apple:CallbackPath"] ?? "/api/auth/external/apple/callback";

        if (new[] { clientId, teamId, keyId, privateKey }.Any(string.IsNullOrWhiteSpace))
        {
            return RedirectWithError(mobileCallback, "Apple OAuth is not configured on API (missing client/team/key/private key settings).");
        }

        var redirectUri = BuildAbsoluteUrl(callbackPath);
        var clientSecret = BuildAppleClientSecret(clientId!, teamId!, keyId!, privateKey!);

        var http = _httpClientFactory.CreateClient();
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = clientId!,
            ["client_secret"] = clientSecret,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code"
        });

        var tokenResponse = await http.PostAsync("https://appleid.apple.com/auth/token", tokenRequest);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            return RedirectWithError(mobileCallback, "Apple token exchange failed.");
        }

        var tokenPayload = await tokenResponse.Content.ReadFromJsonAsync<AppleTokenResponse>();
        var idToken = tokenPayload?.IdToken ?? idTokenFromAuth;

        if (string.IsNullOrWhiteSpace(idToken))
        {
            return RedirectWithError(mobileCallback, "Apple did not return an identity token.");
        }

        var idClaims = ParseJwtPayload(idToken);
        if (idClaims is null)
        {
            return RedirectWithError(mobileCallback, "Apple identity token is invalid.");
        }

        var email = idClaims.Email;
        if (string.IsNullOrWhiteSpace(email))
        {
            if (string.IsNullOrWhiteSpace(idClaims.Subject))
            {
                return RedirectWithError(mobileCallback, "Apple account did not provide an email or stable subject identifier.");
            }

            email = $"{idClaims.Subject}@appleid.local";
        }

        var user = await UpsertExternalUserAsync(email, idClaims.NameOrEmail, "apple");

        return RedirectWithSuccess(mobileCallback, user, "apple");
    }

    private IActionResult RedirectWithSuccess(string mobileCallback, User user, string provider)
    {
        var appToken = BuildAppToken(user);

        var successUrl = QueryHelpers.AddQueryString(mobileCallback, new Dictionary<string, string?>
        {
            ["token"] = appToken,
            ["role"] = user.Role,
            ["userId"] = user.Id.ToString(CultureInfo.InvariantCulture),
            ["provider"] = provider
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

    private string BuildState(string mobileCallback, string provider)
        => _stateProtector.Protect(JsonSerializer.Serialize(new OAuthStatePayload
        {
            MobileCallback = mobileCallback,
            Provider = provider,
            IssuedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16))
        }));

    private OAuthStatePayload? TryResolveState(string? protectedState)
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
                ? payload
                : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<User> UpsertExternalUserAsync(string email, string? fullName, string provider)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        if (user is not null)
        {
            return user;
        }

        user = new User
        {
            FullName = string.IsNullOrWhiteSpace(fullName) ? normalizedEmail : fullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = Hash($"{provider}-{Guid.NewGuid():N}"),
            Role = "Client",
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return user;
    }

    private async Task<User?> TryResolveTokenUserAsync(string token)
    {
        if (!TryParseAppToken(token, out var userId, out var role))
        {
            return null;
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null || !string.Equals(user.Role, role, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return user;
    }

    private static bool TryParseAppToken(string token, out int userId, out string role)
    {
        userId = 0;
        role = string.Empty;

        if (string.IsNullOrWhiteSpace(token) || !token.StartsWith("dev-token-", StringComparison.Ordinal))
        {
            return false;
        }

        var parts = token.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4 || !int.TryParse(parts[2], out userId))
        {
            return false;
        }

        role = parts[3];
        return !string.IsNullOrWhiteSpace(role);
    }

    private static bool IsAllowedMobileCallback(string callback)
        => callback.StartsWith("coachly://auth-callback", StringComparison.OrdinalIgnoreCase);

    private string ResolveMobileCallback(string? mobileCallback)
        => string.IsNullOrWhiteSpace(mobileCallback)
            ? "coachly://auth-callback"
            : mobileCallback;

    private string BuildAbsoluteUrl(string path)
    {
        if (Uri.TryCreate(path, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.ToString();
        }

        var normalizedPath = path.StartsWith('/') ? path : $"/{path}";
        return $"{Request.Scheme}://{Request.Host}{normalizedPath}";
    }

    private static LoginResponseDto ToLoginResponse(User user)
        => new()
        {
            UserId = user.Id,
            Role = user.Role,
            Token = BuildAppToken(user)
        };

    private static string BuildAppToken(User user)
        => $"dev-token-{user.Id}-{user.Role.ToLowerInvariant()}";

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    private static string BuildAppleClientSecret(string clientId, string teamId, string keyId, string privateKey)
    {
        var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(privateKey);
        var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            new Microsoft.IdentityModel.Tokens.ECDsaSecurityKey(ecdsa) { KeyId = keyId },
            Microsoft.IdentityModel.Tokens.SecurityAlgorithms.EcdsaSha256);

        var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Audience = "https://appleid.apple.com",
            Issuer = teamId,
            Subject = new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim("sub", clientId)
            }),
            Expires = DateTime.UtcNow.AddMinutes(10),
            IssuedAt = DateTime.UtcNow,
            SigningCredentials = credentials
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }

    private static ExternalIdentityClaims? ParseJwtPayload(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            var payloadBytes = WebEncoders.Base64UrlDecode(parts[1]);
            var payloadJson = Encoding.UTF8.GetString(payloadBytes);
            return JsonSerializer.Deserialize<ExternalIdentityClaims>(payloadJson);
        }
        catch
        {
            return null;
        }
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

    private sealed class AppleTokenResponse
    {
        [JsonPropertyName("id_token")]
        public string IdToken { get; set; } = string.Empty;
    }

    private sealed class ExternalIdentityClaims
    {
        public string? Email { get; set; }
        public string? Name { get; set; }

        [JsonPropertyName("email_verified")]
        public string? EmailVerified { get; set; }

        [JsonPropertyName("sub")]
        public string? Subject { get; set; }

        [JsonIgnore]
        public string NameOrEmail =>
            string.IsNullOrWhiteSpace(Name)
                ? (Email ?? Subject ?? "Apple User")
                : Name;
    }

    private sealed class OAuthStatePayload
    {
        public string MobileCallback { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public long IssuedAtUnix { get; set; }
        public string Nonce { get; set; } = string.Empty;
    }
}
