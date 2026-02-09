using Trading.Infrastructure.Models;

namespace Trading.Infrastructure.Repositories;

/// <summary>
/// 邮件配置仓储接口
/// </summary>
public interface IEmailConfigRepository
{
    /// <summary>
    /// 获取邮件配置
    /// </summary>
    Task<EmailConfig> GetConfigAsync();

    /// <summary>
    /// 保存邮件配置
    /// </summary>
    Task<EmailConfig> SaveConfigAsync(EmailConfig config);

    /// <summary>
    /// 初始化默认配置
    /// </summary>
    Task InitializeDefaultConfigAsync();
}
