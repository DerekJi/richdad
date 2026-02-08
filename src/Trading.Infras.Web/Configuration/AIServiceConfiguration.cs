using Trading.AI;
using Trading.AI.Services;
using Trading.Infras.Service.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Trading.AI.Configuration;

namespace Trading.Infras.Web.Configuration;

/// <summary>
/// AI服务配置扩展方法
/// </summary>
public static class AIServiceConfiguration
{
    /// <summary>
    /// 注册AI服务（支持多提供商：Azure OpenAI / DeepSeek）
    /// </summary>
    public static IServiceCollection AddAIServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 检查双级AI是否启用
        var dualTierEnabled = configuration.GetValue<bool>("DualTierAI:Enabled");
        if (!dualTierEnabled)
        {
            // AI未启用，不注册任何服务
            return services;
        }

        // 获取AI提供商
        var provider = configuration["DualTierAI:Provider"] ?? "AzureOpenAI";

        // 注册配置
        services.Configure<AzureOpenAISettings>(configuration.GetSection(AzureOpenAISettings.SectionName));
        services.Configure<DeepSeekSettings>(configuration.GetSection(DeepSeekSettings.SectionName));
        services.Configure<MarketAnalysisSettings>(configuration.GetSection(MarketAnalysisSettings.SectionName));
        services.Configure<DualTierAISettings>(configuration.GetSection(DualTierAISettings.SectionName));

        // 注册内存缓存
        services.AddMemoryCache();

        // 根据提供商注册统一AI客户端
        if (provider.Equals("DeepSeek", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IUnifiedAIClient, DeepSeekClientAdapter>();
            services.AddSingleton<IDeepSeekService, DeepSeekService>();
        }
        else // 默认使用 AzureOpenAI
        {
            services.AddSingleton<IUnifiedAIClient, AzureOpenAIClientAdapter>();
            services.AddSingleton<IAzureOpenAIService, AzureOpenAIService>();
        }

        // 注册双级AI服务（支持多提供商）
        services.AddSingleton<IDualTierAIService, MultiProviderDualTierAIService>();
        services.AddSingleton<IDualTierMonitoringService, DualTierMonitoringService>();

        // 注册内部的MarketAnalysisService（不直接暴露）
        services.AddSingleton<MarketAnalysisService>();

        // 注册带持久化的包装器作为IMarketAnalysisService的实现
        services.AddSingleton<IMarketAnalysisService>(serviceProvider =>
        {
            var innerService = serviceProvider.GetRequiredService<MarketAnalysisService>();
            var repository = serviceProvider.GetRequiredService<Trading.Infras.Data.Repositories.IAIAnalysisRepository>();
            var logger = serviceProvider.GetRequiredService<ILogger<MarketAnalysisServiceWithPersistence>>();

            return new MarketAnalysisServiceWithPersistence(innerService, repository, logger);
        });

        return services;
    }
}
