using Trading.AlertSystem.Data.Models;
using Trading.AlertSystem.Data.Repositories;

namespace Trading.AlertSystem.Web.Services;

/// <summary>
/// 内存版本的邮件配置仓储（无需数据库）
/// </summary>
public class InMemoryEmailConfigRepository : IEmailConfigRepository
{
    private EmailConfig? _config;
    private readonly ILogger<InMemoryEmailConfigRepository> _logger;
    private readonly object _lock = new();

    public InMemoryEmailConfigRepository(ILogger<InMemoryEmailConfigRepository> logger)
    {
        _logger = logger;
    }

    public Task<EmailConfig> GetConfigAsync()
    {
        lock (_lock)
        {
            if (_config == null)
            {
                _config = new EmailConfig
                {
                    Id = "default",
                    Enabled = false,
                    SmtpServer = "smtp.gmail.com",
                    SmtpPort = 587,
                    UseSsl = true,
                    FromEmail = "noreply@example.com",
                    FromName = "Trading System",
                    ToEmails = new List<string>(),
                    OnlyOnTelegramFailure = true
                };
            }
            return Task.FromResult(_config);
        }
    }

    public Task<EmailConfig> SaveConfigAsync(EmailConfig config)
    {
        lock (_lock)
        {
            _config = config;
            _logger.LogInformation("邮件配置已更新（内存）");
            return Task.FromResult(config);
        }
    }

    public Task InitializeDefaultConfigAsync()
    {
        lock (_lock)
        {
            if (_config == null)
            {
                _config = new EmailConfig
                {
                    Id = "default",
                    Enabled = false,
                    SmtpServer = "smtp.gmail.com",
                    SmtpPort = 587,
                    UseSsl = true,
                    FromEmail = "noreply@example.com",
                    FromName = "Trading System",
                    ToEmails = new List<string>(),
                    OnlyOnTelegramFailure = true
                };
                _logger.LogInformation("邮件默认配置已初始化（内存）");
            }
            return Task.CompletedTask;
        }
    }
}
