using System;
using System.Windows.Input;
using Coachly.Services;

namespace Coachly.ViewModels.Auth;

public partial class RegisterViewModel : BaseViewModel
{
#pragma warning disable CA1416
    private readonly AuthService _authService;

    private string _fullName = string.Empty;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _selectedRole = "Client";
    private string _errorMessage = string.Empty;

    public RegisterViewModel(AuthService authService)
    {
        _authService = authService;
        Roles = ["Client", "Coach"];
        RegisterCommand = new Command(async () => await RegisterAsync());
        GoogleSignInCommand = new Command(async () => await GoogleSignInAsync());
        AppleSignInCommand = new Command(async () => await AppleSignInAsync());
        GoToLoginCommand = new Command(async () => await NavigateToAsync("//Login"));
    }

    public List<string> Roles { get; }
    public ICommand RegisterCommand { get; }
    public ICommand GoogleSignInCommand { get; }
    public ICommand AppleSignInCommand { get; }
    public ICommand GoToLoginCommand { get; }

    public string FullName
    {
        get => _fullName;
        set => SetProperty(ref _fullName, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string SelectedRole
    {
        get => _selectedRole;
        set => SetProperty(ref _selectedRole, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    private async Task RegisterAsync()
    {
        ErrorMessage = string.Empty;

        var result = await _authService.RegisterAsync(FullName.Trim(), Email.Trim(), Password.Trim(), SelectedRole);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error ?? "Registration failed.";
            return;
        }

        await ShowAlertAsync("Success", "Your account is ready. Please login.");
        await NavigateToAsync("//Login");
    }

    private async Task GoogleSignInAsync()
    {
        ErrorMessage = string.Empty;

        var result = await _authService.SignInWithGoogleAsync();
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error;
            return;
        }

        var route = string.Equals(_authService.CurrentRole, "Coach", StringComparison.OrdinalIgnoreCase)
            ? "//CoachDashboard"
            : "//ClientHome";

        await NavigateToAsync(route);
    }

    private async Task AppleSignInAsync()
    {
        ErrorMessage = string.Empty;

        var result = await _authService.SignInWithAppleAsync();
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error;
            return;
        }

        var route = string.Equals(_authService.CurrentRole, "Coach", StringComparison.OrdinalIgnoreCase)
            ? "//CoachDashboard"
            : "//ClientHome";

        await NavigateToAsync(route);
    }

    private static Task NavigateToAsync(string route)
    {
        var shell = Shell.Current;
        return shell is null ? Task.CompletedTask : shell.GoToAsync(route);
    }

    private static Task ShowAlertAsync(string title, string message)
    {
        var shell = Shell.Current;
        return shell is null ? Task.CompletedTask : shell.DisplayAlert(title, message, "OK");
    }
#pragma warning restore CA1416
}
