using Trading.Infrastructure.Configuration;
using Trading.Infrastructure.Infrastructure;
using Trading.Infrastructure.Repositories;
using Trading.Services.Configuration;
using Trading.Services.Repositories;
using Trading.Web.Services;

namespace Trading.Web.Configuration;

/// <summary>
/// Azure Table Storage 配置和初始化扩展方法
/// </summary>
public static class AzureTableStorageConfiguration
{
    /// <summary>
    /// 注册 Azure Table Storage 相关服务
    /// </summary>
    public static IServiceCollection AddAzureTableStorageServices(this IServiceCollection services, IConfiguration configuration)
    {
        var tableConfig = configuration.GetSection(AzureTableStorageSettings.SectionName);
        var connectionString = tableConfig["ConnectionString"];
        var enabled = tableConfig.GetValue<bool>("Enabled", false);

        if (string.IsNullOrEmpty(connectionString) || !enabled)
        {
            // Azure Table 未配置，使用内存存储
            return services;
        }

        // 配置 Azure Table Storage 设置
        var tableSettings = new AzureTableStorageSettings
        {
            ConnectionString = connectionString,
            Enabled = enabled,
            PriceMonitorTableName = tableConfig["PriceMonitorTableName"] ?? "PriceMonitor",
            AlertHistoryTableName = tableConfig["AlertHistoryTableName"] ?? "AlertHistory",
            EmaMonitorTableName = tableConfig["EmaMonitorTableName"] ?? "EmaMonitor",
            DataSourceConfigTableName = tableConfig["DataSourceConfigTableName"] ?? "DataSourceConfig",
            EmailConfigTableName = tableConfig["EmailConfigTableName"] ?? "EmailConfig",
            PinBarMonitorTableName = tableConfig["PinBarMonitorTableName"] ?? "PinBarMonitor",
            PinBarSignalTableName = tableConfig["PinBarSignalTableName"] ?? "PinBarSignal",
            AIAnalysisHistoryTableName = tableConfig["AIAnalysisHistoryTableName"] ?? "AIAnalysisHistory"
        };

        services.AddSingleton(tableSettings);
        services.AddSingleton<AzureTableStorageContext>();

        // 注册 Azure Table 仓储
        // TODO: 暂时禁用，等待修复循环依赖
        // services.AddSingleton<IPriceMonitorRepository, AzureTablePriceMonitorRepository>();
        services.AddSingleton<IAlertHistoryRepository, AzureTableAlertHistoryRepository>();
        // TODO: 添加其他 Repository

        // 从配置文件加载默认设置（Azure Table 不需要从数据库加载）
        services.AddSingleton(new DataSourceSettings { Provider = "Oanda" });
        services.AddSingleton(sp =>
        {
            var emailConfig = configuration.GetSection("Email");
            return new EmailSettings
            {
                Enabled = emailConfig.GetValue<bool>("Enabled", false),
                SmtpServer = emailConfig["SmtpServer"] ?? "smtp.gmail.com",
                SmtpPort = emailConfig.GetValue<int>("SmtpPort", 587),
                UseSsl = emailConfig.GetValue<bool>("UseSsl", true),
                FromEmail = emailConfig["FromEmail"] ?? "noreply@example.com",
                FromName = emailConfig["FromName"] ?? "Trading System",
                Username = emailConfig["Username"] ?? string.Empty,
                Password = emailConfig["Password"] ?? string.Empty,
                ToEmails = emailConfig.GetSection("ToEmails").Get<List<string>>() ?? new List<string>(),
                OnlyOnTelegramFailure = emailConfig.GetValue<bool>("OnlyOnTelegramFailure", true)
            };
        });

        return services;
    }

    /// <summary>
    /// 初始化 Azure Table Storage
    /// </summary>
    public static async Task InitializeAzureTableStorageAsync(this WebApplication app)
    {
        var tableContext = app.Services.GetService<AzureTableStorageContext>();
        if (tableContext == null)
        {
            var logger = app.Services.GetService<ILogger<Program>>();
            logger?.LogInformation("ℹ️ Azure Table Storage 未配置");
            return;
        }

        try
        {
            await tableContext.InitializeAsync();

            var logger = app.Services.GetService<ILogger<Program>>();
            logger?.LogInformation("✅ Azure Table Storage 初始化成功");
        }
        catch (Exception ex)
        {
            var logger = app.Services.GetService<ILogger<Program>>();
            logger?.LogError(ex, "❌ Azure Table Storage 初始化失败");
            throw;
        }
    }
}
