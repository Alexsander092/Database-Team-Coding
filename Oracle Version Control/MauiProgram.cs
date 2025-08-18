using Microsoft.Extensions.Logging;
using Oracle_Version_Control.Services;
using Oracle_Version_Control.ViewModels;
using Oracle_Version_Control.Views;
using UraniumUI;

namespace Oracle_Version_Control;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseUraniumUI()
            .UseUraniumUIMaterial()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<OracleService>();
        builder.Services.AddSingleton<SettingsService>();

        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<ObjectDetailViewModel>();

        builder.Services.AddTransient<LoginPage>(provider => 
            new LoginPage(provider.GetRequiredService<LoginViewModel>()));
            
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<ObjectDetailPage>();

        return builder.Build();
    }
}