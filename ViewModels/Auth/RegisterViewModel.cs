using System.Windows.Input;
using Coachly.Services;

namespace Coachly.ViewModels.Auth;

public class RegisterViewModel : BaseViewModel
{
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
        GoToLoginCommand = new Command(async () => await Shell.Current.GoToAsync("//Login"));
    }

    public List<string> Roles { get; }
    public ICommand RegisterCommand { get; }
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

        var result = await _authService.RegisterAsync(FullName, Email, Password, SelectedRole);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error ?? "Registration failed.";
            return;
        }

        await Shell.Current.DisplayAlert("Success", "Your account is ready. Please login.", "OK");
        await Shell.Current.GoToAsync("//Login");
    }
}
