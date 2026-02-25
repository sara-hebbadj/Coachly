using System.Windows.Input;
using Coachly.Services;
using Coachly.Shared.DTOs;

namespace Coachly.ViewModels.Auth;

public class LoginViewModel : BaseViewModel
{
#pragma warning disable CA1416
    private readonly AuthService _authService;

    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;

    public LoginViewModel(AuthService authService)
    {
        _authService = authService;
        LoginCommand = new Command(async () => await LoginAsync(), () => !IsBusy);
        GoogleSignInCommand = new Command(async () => await GoogleSignInAsync(), () => !IsBusy);
        AppleSignInCommand = new Command(async () => await AppleSignInAsync(), () => !IsBusy);
        GoToRegisterCommand = new Command(async () => await Shell.Current.GoToAsync("//Register"));
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

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                (LoginCommand as Command)?.ChangeCanExecute();
                (GoogleSignInCommand as Command)?.ChangeCanExecute();
                (AppleSignInCommand as Command)?.ChangeCanExecute();
            }
        }
    }

    public ICommand LoginCommand { get; }
    public ICommand GoogleSignInCommand { get; }
    public ICommand AppleSignInCommand { get; }
    public ICommand GoToRegisterCommand { get; }

    private async Task LoginAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        var response = await _authService.LoginAsync(new LoginRequestDto
        {
            Email = Email,
            Password = Password
        });

        IsBusy = false;

        if (response is null)
        {
            ErrorMessage = "Invalid email or password.";
            return;
        }

        var route = string.Equals(response.Role, "Coach", StringComparison.OrdinalIgnoreCase)
            ? "//CoachDashboard"
            : "//ClientHome";

        await Shell.Current.GoToAsync(route);
    }

    private async Task GoogleSignInAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;

        var result = await _authService.SignInWithGoogleAsync();

        IsBusy = false;

        if (!result.IsSuccess)
        {
            await Shell.Current.DisplayAlert("Google Sign-In", result.Error, "OK");
            return;
        }

        var route = string.Equals(_authService.CurrentRole, "Coach", StringComparison.OrdinalIgnoreCase)
            ? "//CoachDashboard"
            : "//ClientHome";

        await Shell.Current.GoToAsync(route);
    }

    private async Task AppleSignInAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;

        var result = await _authService.SignInWithAppleAsync();

        IsBusy = false;

        if (!result.IsSuccess)
        {
            await Shell.Current.DisplayAlert("Apple Sign-In", result.Error, "OK");
            return;
        }

        var route = string.Equals(_authService.CurrentRole, "Coach", StringComparison.OrdinalIgnoreCase)
            ? "//CoachDashboard"
            : "//ClientHome";

        await Shell.Current.GoToAsync(route);
    }
#pragma warning restore CA1416
}
