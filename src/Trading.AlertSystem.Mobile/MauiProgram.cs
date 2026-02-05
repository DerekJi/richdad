using Microsoft.Extensions.Logging;
using Trading.AlertSystem.Mobile.Services;
using Trading.AlertSystem.Mobile.ViewModels;
using Trading.AlertSystem.Mobile.Views;

namespace Trading.AlertSystem.Mobile;

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

        // 注册服务
        builder.Services.AddSingleton<IAlertApiClient, AlertApiClient>();
        builder.Services.AddSingleton<ISettingsService, SettingsService>();

        // 注册 ViewModels
        builder.Services.AddTransient<MonitorStatusViewModel>();
        builder.Services.AddTransient<AlertListViewModel>();
        builder.Services.AddTransient<AlertDetailViewModel>();
        builder.Services.AddTransient<AlertHistoryViewModel>();
        builder.Services.AddTransient<EmaConfigViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        // 注册 Pages
        builder.Services.AddTransient<MonitorStatusPage>();
        builder.Services.AddTransient<AlertListPage>();
        builder.Services.AddTransient<AlertDetailPage>();
        builder.Services.AddTransient<AlertHistoryPage>();
        builder.Services.AddTransient<EmaConfigPage>();
        builder.Services.AddTransient<SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
