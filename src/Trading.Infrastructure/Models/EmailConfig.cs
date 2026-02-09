using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Trading.Infrastructure.Models;

/// <summary>
/// 邮件配置（存储在数据库）
/// </summary>
public class EmailConfig
{
    /// <summary>
    /// 配置ID（固定为"email-config"）
    /// </summary>
    [JsonPropertyName("id")]  // For System.Text.Json
    [JsonProperty("id")]      // For Newtonsoft.Json (CosmosDB)
    public string Id { get; set; } = "email-config";

    /// <summary>
    /// 是否启用邮件通知
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// SMTP服务器地址
    /// </summary>
    public string SmtpServer { get; set; } = "smtp-mail.outlook.com";

    /// <summary>
    /// SMTP端口号
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// 是否使用SSL
    /// </summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// 发件人邮箱
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// 发件人名称
    /// </summary>
    public string FromName { get; set; } = "Trading Alert System";

    /// <summary>
    /// SMTP用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// SMTP密码
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 收件人邮箱列表
    /// </summary>
    public List<string> ToEmails { get; set; } = new();

    /// <summary>
    /// 仅在Telegram失败时发送邮件
    /// </summary>
    public bool OnlyOnTelegramFailure { get; set; } = true;

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
