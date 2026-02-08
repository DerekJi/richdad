namespace Trading.Infras.Data.Configuration;

/// <summary>
/// Telegram Bot配置
/// </summary>
public class TelegramSettings
{
    /// <summary>
    /// Telegram Bot Token
    /// </summary>
    public string BotToken { get; set; } = string.Empty;

    /// <summary>
    /// 默认接收消息的Chat ID
    /// </summary>
    public long? DefaultChatId { get; set; }

    /// <summary>
    /// 是否启用Telegram通知
    /// </summary>
    public bool Enabled { get; set; } = true;
}
