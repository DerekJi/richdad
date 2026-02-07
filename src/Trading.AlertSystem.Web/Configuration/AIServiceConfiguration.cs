using Trading.AI;
using Trading.AI.Services;
using Trading.AlertSystem.Service.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Trading.AI.Configuration;

namespace Trading.AlertSystem.Web.Configuration;

/// <summary>
/// AI服务配置扩展方法
/// </summary>
public static class AIServiceConfiguration
{
    /// <summary>
    /// 注册AI服务（可选）
    /// </summary>
    public static IServiceCollection AddAIServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 检查是否启用AI功能
        var enabledValue = configuration["AzureOpenAI:Enabled"];
        var isEnabled = bool.TryParse(enabledValue, out var enabled) && enabled;

        if (!isEnabled)
        {
            // AI未启用，不注册任何服务
            return services;
        }

        // 注册配置
        services.Configure<AzureOpenAISettings>(configuration.GetSection(AzureOpenAISettings.SectionName));
        services.Configure<MarketAnalysisSettings>(configuration.GetSection(MarketAnalysisSettings.SectionName));
        services.Configure<DualTierAISettings>(configuration.GetSection(DualTierAISettings.SectionName));

        // 注册内存缓存
        services.AddMemoryCache();

        // 注册底层AI服务
        services.AddSingleton<IAzureOpenAIService, AzureOpenAIService>();

        // 注册双级AI服务
        services.AddSingleton<IDualTierAIService, DualTierAIService>();
        services.AddSingleton<IDualTierMonitoringService, DualTierMonitoringService>();

        // 注册内部的MarketAnalysisService（不直接暴露）
        services.AddSingleton<MarketAnalysisService>();

        // 注册带持久化的包装器作为IMarketAnalysisService的实现
        services.AddSingleton<IMarketAnalysisService>(serviceProvider =>
        {
            var innerService = serviceProvider.GetRequiredService<MarketAnalysisService>();
            var repository = serviceProvider.GetRequiredService<Trading.AlertSystem.Data.Repositories.IAIAnalysisRepository>();
            var logger = serviceProvider.GetRequiredService<ILogger<MarketAnalysisServiceWithPersistence>>();

            return new MarketAnalysisServiceWithPersistence(innerService, repository, logger);
        });

        return services;
    }
}
