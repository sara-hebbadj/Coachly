using Coachly.Services;

namespace Coachly;

public partial class AppShell : Shell
{
#pragma warning disable CA1416

    private readonly AuthService _authService;

    public AppShell(AuthService authService)
    {
        InitializeComponent();
        _authService = authService;
        _authService.AuthStateChanged += HandleAuthStateChanged;

        HandleAuthStateChanged();
    }

    private void HandleAuthStateChanged()
    {
        var isAuthenticated = _authService.IsAuthenticated;

        AuthGroup.IsVisible = !isAuthenticated;
        ClientGroup.IsVisible = isAuthenticated && string.Equals(_authService.CurrentRole, "Client", StringComparison.Ordinal);
        CoachGroup.IsVisible = isAuthenticated && string.Equals(_authService.CurrentRole, "Coach", StringComparison.Ordinal);
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        _authService.Logout();
        await Shell.Current.GoToAsync("//Login");
    }
#pragma warning restore CA1416
}
