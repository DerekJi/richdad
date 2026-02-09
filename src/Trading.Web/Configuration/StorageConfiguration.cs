using Trading.Infrastructure.Configuration;
using Trading.Infrastructure.Infrastructure;
using Trading.Infrastructure.Repositories;
using Trading.Services.Configuration;
using Trading.Services.Repositories;
using Trading.Web.Services;

namespace Trading.Web.Configuration;

/// <summary>
/// 存储层配置扩展方法
/// </summary>
public static class StorageConfiguration
{
    /// <summary>
    /// 添加存储层后备方案（当 Azure Table 和 Cosmos DB 都未配置时使用内存存储）
    /// </summary>
    public static IServiceCollection AddStorageFallback(this IServiceCollection services, IConfiguration configuration)
    {
        // 检查是否已注册了 Azure Table Storage
        var hasAzureTable = !string.IsNullOrEmpty(
            configuration.GetSection("AzureTableStorage")["ConnectionString"]) &&
            configuration.GetSection("AzureTableStorage").GetValue<bool>("Enabled", false);

        // 检查是否已注册了 Cosmos DB（已禁用，但保留检查逻辑）
        var hasCosmosDb = !string.IsNullOrEmpty(
            configuration.GetSection("CosmosDb")["ConnectionString"]);

        if (!hasAzureTable && !hasCosmosDb)
        {
            // 两者都未配置，使用内存存储
            RegisterInMemoryRepositories(services);

            var logger = services.BuildServiceProvider().GetService<ILogger<Program>>();
            logger?.LogWarning("⚠️ 未配置持久化存储，使用内存模式。生产环境请配置 Azure Table Storage。");
        }
        else if (hasAzureTable)
        {
            // Azure Table 已配置但可能没有注册所有仓储，补充缺失的 InMemory 仓储
            // 这是因为某些 Azure Table 仓储还未实现
            // TODO: 当所有 Azure Table 仓储实现后，移除这部分代码
            RegisterMissingInMemoryRepositories(services);

            var logger = services.BuildServiceProvider().GetService<ILogger<Program>>();
            logger?.LogInformation("✅ 使用 Azure Table Storage（部分功能使用内存后备）");
        }

        return services;
    }

    /// <summary>
    /// 注册所有内存仓储
    /// </summary>
    private static void RegisterInMemoryRepositories(IServiceCollection services)
    {
        services.AddSingleton<IPriceMonitorRepository, InMemoryPriceMonitorRepository>();
        services.AddSingleton<IAlertHistoryRepository, InMemoryAlertHistoryRepository>();
        services.AddSingleton<IEmaMonitorRepository, InMemoryEmaMonitorRepository>();
        services.AddSingleton<IDataSourceConfigRepository, InMemoryDataSourceConfigRepository>();
        services.AddSingleton<IEmailConfigRepository, InMemoryEmailConfigRepository>();
        services.AddSingleton<IPinBarMonitorRepository, InMemoryPinBarMonitorRepository>();
        services.AddSingleton<IAIAnalysisRepository, InMemoryAIAnalysisRepository>();

        // 使用默认配置
        services.AddSingleton(new DataSourceSettings { Provider = "Oanda" });
        services.AddSingleton(new EmailSettings
        {
            Enabled = false,
            SmtpServer = "smtp.gmail.com",
            SmtpPort = 587,
            UseSsl = true,
            FromEmail = "noreply@example.com",
            FromName = "Trading System",
            OnlyOnTelegramFailure = true
        });
    }

    /// <summary>
    /// 注册缺失的内存仓储（Azure Table 部分功能未实现时的后备）
    /// </summary>
    private static void RegisterMissingInMemoryRepositories(IServiceCollection services)
    {
        // 只注册那些在 Azure Table 中还未实现的仓储
        // IPriceMonitorRepository - Azure Table 实现被禁用，使用内存版本
        if (!services.Any(sd => sd.ServiceType == typeof(IPriceMonitorRepository)))
        {
            services.AddSingleton<IPriceMonitorRepository, InMemoryPriceMonitorRepository>();
        }

        // 其他仓储如果未注册也添加内存版本
        if (!services.Any(sd => sd.ServiceType == typeof(IEmaMonitorRepository)))
        {
            services.AddSingleton<IEmaMonitorRepository, InMemoryEmaMonitorRepository>();
        }

        if (!services.Any(sd => sd.ServiceType == typeof(IDataSourceConfigRepository)))
        {
            services.AddSingleton<IDataSourceConfigRepository, InMemoryDataSourceConfigRepository>();
        }

        if (!services.Any(sd => sd.ServiceType == typeof(IEmailConfigRepository)))
        {
            services.AddSingleton<IEmailConfigRepository, InMemoryEmailConfigRepository>();
        }

        if (!services.Any(sd => sd.ServiceType == typeof(IPinBarMonitorRepository)))
        {
            services.AddSingleton<IPinBarMonitorRepository, InMemoryPinBarMonitorRepository>();
        }

        if (!services.Any(sd => sd.ServiceType == typeof(IAIAnalysisRepository)))
        {
            services.AddSingleton<IAIAnalysisRepository, InMemoryAIAnalysisRepository>();
        }
    }

    /// <summary>
    /// 初始化存储层
    /// </summary>
    public static async Task InitializeStorageAsync(this WebApplication app)
    {
        // 初始化 Azure Table Storage（如果配置了）
        var tableContext = app.Services.GetService<AzureTableStorageContext>();
        if (tableContext != null)
        {
            await app.InitializeAzureTableStorageAsync();
            var logger = app.Services.GetService<ILogger<Program>>();
            logger?.LogInformation("✅ 使用 Azure Table Storage 作为持久化存储");
            return;
        }

        // Cosmos DB 已禁用
        // 如果将来需要启用，取消下面的注释
        /*
        var cosmosContext = app.Services.GetService<CosmosDbContext>();
        if (cosmosContext != null)
        {
            await app.InitializeCosmosDbAsync();
            var logger = app.Services.GetService<ILogger<Program>>();
            logger?.LogInformation("✅ 使用 Cosmos DB 作为持久化存储");
            return;
        }
        */

        // 使用内存存储
        var logger2 = app.Services.GetService<ILogger<Program>>();
        logger2?.LogInformation("ℹ️ 使用内存存储模式（数据重启后丢失）");
    }
}
