using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trading.AI.Configuration;
using Trading.AI.Services;

namespace Trading.AI;

/// <summary>
/// Trading.AI服务注册扩展
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加Trading.AI服务（可选）
    /// </summary>
    public static IServiceCollection AddTradingAI(this IServiceCollection services, IConfiguration configuration)
    {
        // 注册配置（始终注册，即使未启用）
        services.Configure<AzureOpenAISettings>(configuration.GetSection(AzureOpenAISettings.SectionName));
        services.Configure<MarketAnalysisSettings>(configuration.GetSection(MarketAnalysisSettings.SectionName));

        // 检查是否启用AI功能
        var enabledValue = configuration["AzureOpenAI:Enabled"];
        var isEnabled = bool.TryParse(enabledValue, out var enabled) && enabled;

        if (!isEnabled)
        {
            // AI未启用，不注册服务（系统可正常运行，但无AI功能）
            return services;
        }

        // 注册内存缓存
        services.AddMemoryCache();

        // 注册AI服务（仅当启用时）
        services.AddSingleton<IAzureOpenAIService, AzureOpenAIService>();
        services.AddSingleton<IMarketAnalysisService, MarketAnalysisService>();

        return services;
    }
}
