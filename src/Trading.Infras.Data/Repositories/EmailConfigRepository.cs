using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Trading.AlertSystem.Data.Infrastructure;
using Trading.AlertSystem.Data.Models;

namespace Trading.AlertSystem.Data.Repositories;

/// <summary>
/// 邮件配置仓储实现
/// </summary>
public class EmailConfigRepository : IEmailConfigRepository
{
    private readonly CosmosDbContext _context;
    private readonly ILogger<EmailConfigRepository> _logger;
    private const string ConfigId = "email-config";

    public EmailConfigRepository(CosmosDbContext context, ILogger<EmailConfigRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<EmailConfig> GetConfigAsync()
    {
        try
        {
            var container = _context.GetEmailConfigContainer();
            var response = await container.ReadItemAsync<EmailConfig>(
                ConfigId,
                new PartitionKey(ConfigId)
            );
            var config = response.Resource;

            // 确保 SmtpServer 有有效值（可能从旧版本迁移时为空）
            if (string.IsNullOrWhiteSpace(config.SmtpServer))
            {
                config.SmtpServer = "smtp-mail.outlook.com";
                _logger.LogWarning("SMTP服务器地址为空，已使用默认值: {SmtpServer}", config.SmtpServer);
            }

            return config;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogInformation("邮件配置不存在，创建默认配置");
            return await InitializeAndReturnDefaultConfigAsync();
        }
    }

    public async Task<EmailConfig> SaveConfigAsync(EmailConfig config)
    {
        config.Id = ConfigId;
        config.LastUpdated = DateTime.UtcNow;

        var container = _context.GetEmailConfigContainer();
        var response = await container.UpsertItemAsync(
            config,
            new PartitionKey(ConfigId)
        );

        _logger.LogInformation("邮件配置已保存");
        return response.Resource;
    }

    public async Task InitializeDefaultConfigAsync()
    {
        try
        {
            var existing = await GetConfigAsync();
            _logger.LogInformation("邮件配置已存在，跳过初始化");
        }
        catch
        {
            await InitializeAndReturnDefaultConfigAsync();
        }
    }

    private async Task<EmailConfig> InitializeAndReturnDefaultConfigAsync()
    {
        var defaultConfig = new EmailConfig
        {
            Id = ConfigId,
            Enabled = false,
            SmtpServer = "smtp-mail.outlook.com",
            SmtpPort = 587,
            UseSsl = true,
            FromName = "Trading Alert System",
            OnlyOnTelegramFailure = true,
            LastUpdated = DateTime.UtcNow
        };

        return await SaveConfigAsync(defaultConfig);
    }
}
