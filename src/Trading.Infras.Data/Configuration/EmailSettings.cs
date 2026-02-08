namespace Trading.Infras.Data.Configuration;

/// <summary>
/// 邮件通知设置
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// 是否启用邮件通知
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// SMTP服务器地址
    /// </summary>
    public string SmtpServer { get; set; } = string.Empty;

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
    /// SMTP用户名（通常是邮箱地址）
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// SMTP密码或应用专用密码
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 收件人邮箱列表
    /// </summary>
    public List<string> ToEmails { get; set; } = new();

    /// <summary>
    /// 仅在Telegram失败时发送邮件
    /// </summary>
    public bool OnlyOnTelegramFailure { get; set; } = false;
}
