using Trading.AlertSystem.Data.Configuration;
using Trading.AlertSystem.Data.Infrastructure;
using Trading.AlertSystem.Data.Repositories;
using Trading.AlertSystem.Service.Configuration;
using Trading.AlertSystem.Service.Repositories;
using Trading.AlertSystem.Web.Services;

namespace Trading.AlertSystem.Web.Configuration;

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
            // 如果没有CosmosDB，使用默认配置
            services.AddSingleton(new DataSourceSettings { Provider = "Oanda" });
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
