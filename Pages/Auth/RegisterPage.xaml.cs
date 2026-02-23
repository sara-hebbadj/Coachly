using Coachly.Helpers;
using Coachly.ViewModels.Auth;

namespace Coachly.Pages.Auth;

public partial class RegisterPage : ContentPage
{
    public RegisterPage()
    {
        InitializeComponent();
        BindingContext = ServiceHelper.GetService<RegisterViewModel>();
    }
}
