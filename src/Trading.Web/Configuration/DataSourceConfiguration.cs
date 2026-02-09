using Trading.Infrastructure.Services;
using Trading.Web.Services;

namespace Trading.Web.Configuration;

/// <summary>
/// 数据源服务配置扩展方法
/// </summary>
public static class DataSourceConfiguration
{
    /// <summary>
    /// 注册数据源服务（TradeLocker, Oanda, MarketData）
    /// </summary>
    public static IServiceCollection AddDataSourceServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 注册 TradeLocker 服务
        var tradeLockerConfig = configuration.GetSection("TradeLocker");
        if (!string.IsNullOrEmpty(tradeLockerConfig["Email"]) && !string.IsNullOrEmpty(tradeLockerConfig["Password"]))
        {
            services.AddHttpClient<ITradeLockerService, TradeLockerService>();
        }
        else
        {
            services.AddSingleton<ITradeLockerService, DemoTradeLockerService>();
        }

        // 注册 OANDA 服务
        var oandaConfig = configuration.GetSection("Oanda");
        if (!string.IsNullOrEmpty(oandaConfig["ApiKey"]) && !string.IsNullOrEmpty(oandaConfig["AccountId"]))
        {
            services.AddHttpClient<IOandaService, OandaService>();
            services.AddSingleton<IOandaStreamingService, OandaStreamingService>();
        }

        // 注册统一的市场数据服务（根据配置自动路由）
        services.AddSingleton<IMarketDataService, MarketDataService>();

        return services;
    }
}
