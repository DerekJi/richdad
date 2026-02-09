using Microsoft.Extensions.Options;
using Trading.Infrastructure.Configuration;
using Trading.Services.Configuration;

namespace Trading.Web.Configuration;

/// <summary>
/// 应用程序设置配置扩展方法
/// </summary>
public static class SettingsConfiguration
{
    /// <summary>
    /// 注册所有应用程序设置
    /// </summary>
    public static IServiceCollection AddApplicationSettings(this IServiceCollection services, IConfiguration configuration)
    {
        // 数据源设置
        services.Configure<DataSourceSettings>(configuration.GetSection("DataSource"));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<DataSourceSettings>>().Value);

        // TradeLocker 设置
        services.Configure<TradeLockerSettings>(configuration.GetSection("TradeLocker"));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<TradeLockerSettings>>().Value);

        // Oanda 设置
        services.Configure<OandaSettings>(configuration.GetSection("Oanda"));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<OandaSettings>>().Value);

        // Telegram 设置
        services.Configure<TelegramSettings>(configuration.GetSection("Telegram"));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<TelegramSettings>>().Value);

        // Email 设置
        services.Configure<EmailSettings>(configuration.GetSection("Email"));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<EmailSettings>>().Value);

        // 监控设置
        services.Configure<MonitoringSettings>(configuration.GetSection("Monitoring"));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<MonitoringSettings>>().Value);

        // EMA 监控设置
        services.Configure<EmaMonitoringSettings>(configuration.GetSection("EmaMonitoring"));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<EmaMonitoringSettings>>().Value);

        return services;
    }
}
