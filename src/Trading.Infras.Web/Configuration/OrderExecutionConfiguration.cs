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

        // 根据配置选择订单执行服务
        if (!settings.EnableRealTrading)
        {
            // 安全模式：强制使用Mock
            services.AddSingleton<IOrderExecutionService, MockOrderExecutionService>();
        }
        else
        {
            // 根据Provider选择真实平台
            switch (settings.Provider.ToLowerInvariant())
            {
                case "oanda":
                    services.AddSingleton<IOrderExecutionService, OandaOrderAdapter>();
                    break;

                case "tradelocker":
                    services.AddSingleton<IOrderExecutionService, TradeLockerOrderAdapter>();
                    break;

                case "mock":
                default:
                    services.AddSingleton<IOrderExecutionService, MockOrderExecutionService>();
                    break;
            }
        }

        return services;
    }
}
