using Trading.Core.Trading;
using Trading.Infras.Service.Adapters;
using Trading.Infras.Service.Configuration;

namespace Trading.Infras.Web.Configuration;

/// <summary>
/// 订单执行服务配置扩展方法
/// </summary>
public static class OrderExecutionConfiguration
{
    /// <summary>
    /// 注册订单执行服务（根据配置自动选择平台）
    /// </summary>
    public static IServiceCollection AddOrderExecutionService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 读取配置
        var settings = configuration.GetSection("OrderExecution").Get<OrderExecutionSettings>()
            ?? new OrderExecutionSettings();

        // 注册配置
        services.AddSingleton(settings);

        // 创建临时logger用于启动日志
        using var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetService<ILogger<Program>>();

        // 根据配置选择订单执行服务
        if (!settings.EnableRealTrading)
        {
            // 安全模式：强制使用Mock
            services.AddSingleton<IOrderExecutionService, MockOrderExecutionService>();
            logger?.LogWarning("⚠️ 订单执行: 模拟模式（真实交易已禁用: EnableRealTrading=false）");
        }
        else
        {
            // 根据Provider选择真实平台
            switch (settings.Provider.ToLowerInvariant())
            {
                case "oanda":
                    services.AddSingleton<IOrderExecutionService, OandaOrderAdapter>();
                    logger?.LogInformation("✅ 订单执行平台: Oanda (真实交易模式)");
                    break;

                case "tradelocker":
                    services.AddSingleton<IOrderExecutionService, TradeLockerOrderAdapter>();
                    logger?.LogInformation("✅ 订单执行平台: TradeLocker (真实交易模式)");
                    break;

                case "mock":
                    services.AddSingleton<IOrderExecutionService, MockOrderExecutionService>();
                    logger?.LogWarning("⚠️ 订单执行: 模拟模式（Provider=Mock）");
                    break;

                default:
                    services.AddSingleton<IOrderExecutionService, MockOrderExecutionService>();
                    logger?.LogWarning("⚠️ 未知的订单执行平台: {Platform}，使用模拟模式", settings.Provider);
                    break;
            }
        }

        return services;
    }
}
