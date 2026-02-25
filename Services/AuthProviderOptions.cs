namespace Coachly.Services;

public class AuthProviderOptions
{
    public bool EnableExternalProviderSignIn { get; init; } = false;
    public string GoogleClientId { get; init; } = string.Empty;
    public string AppleClientId { get; init; } = string.Empty;
    public string BackendOAuthStartUrl { get; init; } = string.Empty;

    public bool IsGoogleConfigured =>
        !string.IsNullOrWhiteSpace(GoogleClientId)
        && !string.IsNullOrWhiteSpace(BackendOAuthStartUrl);

    public bool IsAppleConfigured =>
        !string.IsNullOrWhiteSpace(AppleClientId)
        && !string.IsNullOrWhiteSpace(BackendOAuthStartUrl);
}
