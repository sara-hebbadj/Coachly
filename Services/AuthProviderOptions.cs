namespace Coachly.Services;

public class AuthProviderOptions
{
    public bool EnableExternalProviderSignIn { get; init; } = false;
    public string BackendOAuthStartUrl { get; init; } = string.Empty;
    public string MobileAuthCallbackUri { get; init; } = "coachly://auth-callback";

    public bool IsGoogleConfigured =>
        !string.IsNullOrWhiteSpace(BackendOAuthStartUrl)
        && !string.IsNullOrWhiteSpace(MobileAuthCallbackUri);

    public bool IsAppleConfigured =>
        !string.IsNullOrWhiteSpace(BackendOAuthStartUrl)
        && !string.IsNullOrWhiteSpace(MobileAuthCallbackUri);
}
