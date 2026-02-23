namespace Coachly.Helpers;

public static class ServiceHelper
{
    public static T? GetService<T>() where T : class
        => Application.Current?.Handler?.MauiContext?.Services.GetService<T>();
}
