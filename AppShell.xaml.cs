using Coachly.Services;

namespace Coachly;

public partial class AppShell : Shell
{
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
        ClientGroup.IsVisible = isAuthenticated && _authService.CurrentRole == "Client";
        CoachGroup.IsVisible = isAuthenticated && _authService.CurrentRole == "Coach";
    }
}
