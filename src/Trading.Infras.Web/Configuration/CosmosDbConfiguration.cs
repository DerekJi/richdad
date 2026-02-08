using Trading.Infras.Data.Configuration;
using Trading.Infras.Data.Infrastructure;
using Trading.Infras.Data.Repositories;
using Trading.Infras.Service.Configuration;
using Trading.Infras.Service.Repositories;
using Trading.Infras.Web.Services;

namespace Trading.Infras.Web.Configuration;

/// <summary>
/// CosmosDB 配置和初始化扩展方法
/// </summary>
public static class CosmosDbConfiguration
{
    /// <summary>
    /// 注册 CosmosDB 相关服务
    /// </summary>
    public static IServiceCollection AddCosmosDbServices(this IServiceCollection services, IConfiguration configuration)
    {
        var cosmosConfig = configuration.GetSection("CosmosDb");
        var connectionString = cosmosConfig["ConnectionString"];

        if (string.IsNullOrEmpty(connectionString))
        {
            // 使用内存存储作为后备方案
            services.AddSingleton<IPriceMonitorRepository, InMemoryPriceMonitorRepository>();
            services.AddSingleton<IAlertHistoryRepository, InMemoryAlertHistoryRepository>();
            services.AddSingleton<IEmaMonitorRepository, InMemoryEmaMonitorRepository>();
            services.AddSingleton<IDataSourceConfigRepository, InMemoryDataSourceConfigRepository>();
            services.AddSingleton<IEmailConfigRepository, InMemoryEmailConfigRepository>();
            services.AddSingleton<IPinBarMonitorRepository, InMemoryPinBarMonitorRepository>();
            services.AddSingleton<IAIAnalysisRepository, InMemoryAIAnalysisRepository>();

            // 使用默认配置（不依赖数据库）
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

            return services;
        }

        // 配置 CosmosDB 设置
        var cosmosSettings = new CosmosDbSettings
        {
            ConnectionString = connectionString,
            DatabaseName = cosmosConfig["DatabaseName"] ?? "TradingSystem",
            PriceMonitorContainerName = cosmosConfig["PriceMonitorContainerName"] ?? "PriceMonitor",
            AlertHistoryContainerName = cosmosConfig["AlertHistoryContainerName"] ?? "AlertHistory",
            EmaMonitorContainerName = cosmosConfig["EmaMonitorContainerName"] ?? "EmaMonitor",
            DataSourceConfigContainerName = cosmosConfig["DataSourceConfigContainerName"] ?? "DataSourceConfig",
            EmailConfigContainerName = cosmosConfig["EmailConfigContainerName"] ?? "EmailConfig",
            AIAnalysisHistoryContainerName = cosmosConfig["AIAnalysisHistoryContainerName"] ?? "AIAnalysisHistory"
        };

        services.AddSingleton(cosmosSettings);
        services.AddSingleton<CosmosDbContext>();

        // 注册仓储
        services.AddSingleton<IPriceMonitorRepository, PriceMonitorRepository>();
        services.AddSingleton<IAlertHistoryRepository, AlertHistoryRepository>();
        services.AddSingleton<IEmaMonitorRepository, EmaMonitorRepository>();
        services.AddSingleton<IDataSourceConfigRepository, DataSourceConfigRepository>();
        services.AddSingleton<IEmailConfigRepository, EmailConfigRepository>();
        services.AddSingleton<IPinBarMonitorRepository, PinBarMonitorRepository>();
        services.AddSingleton<IAIAnalysisRepository, CosmosAIAnalysisRepository>();

        // 注册延迟初始化的 DataSourceSettings（从数据库加载）
        services.AddSingleton<DataSourceSettings>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetRequiredService<CosmosDbContext>();
            var dataSourceRepo = serviceProvider.GetRequiredService<IDataSourceConfigRepository>();

            // 初始化数据库并加载配置
            dbContext.InitializeAsync().GetAwaiter().GetResult();
            var dataSourceConfig = dataSourceRepo.GetConfigAsync().GetAwaiter().GetResult();

            return new DataSourceSettings { Provider = dataSourceConfig.Provider };
        });

        // 从数据库加载邮件配置
        services.AddSingleton<EmailSettings>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetRequiredService<CosmosDbContext>();
            var emailRepo = serviceProvider.GetRequiredService<IEmailConfigRepository>();

            dbContext.InitializeAsync().GetAwaiter().GetResult();
            var emailConfig = emailRepo.GetConfigAsync().GetAwaiter().GetResult();

            return new EmailSettings
            {
                Enabled = emailConfig.Enabled,
                SmtpServer = emailConfig.SmtpServer,
                SmtpPort = emailConfig.SmtpPort,
                UseSsl = emailConfig.UseSsl,
                FromEmail = emailConfig.FromEmail,
                FromName = emailConfig.FromName,
                Username = emailConfig.Username,
                Password = emailConfig.Password,
                ToEmails = emailConfig.ToEmails,
                OnlyOnTelegramFailure = emailConfig.OnlyOnTelegramFailure
            };
        });

        return services;
    }

    /// <summary>
    /// 初始化 CosmosDB 和默认配置
    /// </summary>
    public static async Task InitializeCosmosDbAsync(this WebApplication app)
    {
        var cosmosDbContext = app.Services.GetService<CosmosDbContext>();
        if (cosmosDbContext == null)
        {
            // 没有 Cosmos DB 配置，跳过初始化
            var logger = app.Services.GetService<ILogger<Program>>();
            logger?.LogInformation("⚠️ Cosmos DB 未配置，使用内存存储模式");
            return;
        }

        await cosmosDbContext.InitializeAsync();

        // 初始化 EMA 配置（如果数据库中不存在）
        var emaMonitorRepo = app.Services.GetService<IEmaMonitorRepository>();
        var emaSettings = app.Services.GetService<EmaMonitoringSettings>();
        if (emaMonitorRepo != null && emaSettings != null)
        {
            await emaMonitorRepo.InitializeDefaultConfigAsync(
                emaSettings.Enabled,
                emaSettings.Symbols,
                emaSettings.TimeFrames,
                emaSettings.EmaPeriods,
                emaSettings.HistoryMultiplier);
        }

        // 初始化邮件配置（如果数据库中不存在）
        var emailConfigRepo = app.Services.GetService<IEmailConfigRepository>();
        if (emailConfigRepo != null)
        {
            await emailConfigRepo.InitializeDefaultConfigAsync();
        }

        // 初始化 PinBar 配置（如果数据库中不存在）
        var pinBarRepo = app.Services.GetService<IPinBarMonitorRepository>();
        if (pinBarRepo != null)
        {
            var existingConfig = await pinBarRepo.GetConfigAsync();
            // 配置会自动创建默认值
        }
    }
}
