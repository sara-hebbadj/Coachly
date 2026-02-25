using Coachly.Services;
using Coachly.ViewModels.Auth;
using Microsoft.Extensions.Logging;

namespace Coachly
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var apiBaseUrl = DeviceInfo.Platform == DevicePlatform.Android
                ? "http://10.0.2.2:5114/"
                : "http://localhost:5114/";

            builder.Services.AddSingleton(_ => new HttpClient
            {
                BaseAddress = new Uri(apiBaseUrl),
                Timeout = TimeSpan.FromSeconds(20)
            });

            builder.Services.AddSingleton(new AuthProviderOptions
            {
                // Copy-paste your real provider values here.
                GoogleClientId = "",
                AppleClientId = "",
                BackendOAuthStartUrl = apiBaseUrl,
                EnableExternalProviderSignIn = true
            });

            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<RegisterViewModel>();

            return builder.Build();
        }


    }

}
