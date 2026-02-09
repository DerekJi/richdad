using Trading.Services.Services;
using Trading.Web.Services;

namespace Trading.Web.Configuration;

/// <summary>
/// 业务服务和后台服务配置扩展方法
/// </summary>
public static class BusinessServiceConfiguration
{
    /// <summary>
    /// 注册业务服务
    /// </summary>
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddSingleton<IPriceMonitorService, PriceMonitorService>();
        services.AddSingleton<IChartService, ChartService>();
        services.AddSingleton<IEmaMonitoringService, EmaMonitoringService>();
        services.AddSingleton<IStreamingPriceMonitorService, StreamingPriceMonitorService>();
        services.AddSingleton<IPriceCacheService, PriceCacheService>();
        services.AddSingleton<PinBarMonitoringService>();

        // 技术指标和形态识别服务 - Issue #7
        services.AddSingleton<TechnicalIndicatorService>();
        services.AddScoped<PatternRecognitionService>();

        return services;
    }

    /// <summary>
    /// 注册后台服务
    /// </summary>
    public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        // 使用 Streaming 版本替代轮询版本
        services.AddHostedService<StreamingPriceMonitorHostedService>();

        // EMA 监测后台服务
        services.AddHostedService<EmaMonitoringHostedService>();

        // PinBar 监控后台服务 - 使用双级AI版本（成本优化）
        services.AddHostedService<DualTierPinBarMonitoringService>();

        return services;
    }
}
