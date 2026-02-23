using Coachly.Helpers;
using Coachly.ViewModels.Auth;

namespace Coachly.Pages.Auth;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
        BindingContext = ServiceHelper.GetService<LoginViewModel>();
    }
}
