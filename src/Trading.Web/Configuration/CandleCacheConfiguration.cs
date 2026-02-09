using Trading.Infrastructure.Configuration;
using Trading.Infrastructure.Repositories;
using Trading.Services.Services;

namespace Trading.Web.Configuration;

/// <summary>
/// 市场数据缓存服务配置扩展方法
/// </summary>
public static class CandleCacheConfiguration
{
    /// <summary>
    /// 注册市场数据缓存相关服务
    /// </summary>
    public static IServiceCollection AddCandleCacheServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 注册配置
        services.Configure<CandleCacheSettings>(
            configuration.GetSection(CandleCacheSettings.SectionName));

        // 注册 Repository
        services.AddSingleton<ICandleRepository, CandleRepository>();

        // 注册缓存服务
        services.AddSingleton<CandleCacheService>();

        // 注册初始化服务
        services.AddSingleton<CandleInitializationService>();

        return services;
    }
}
