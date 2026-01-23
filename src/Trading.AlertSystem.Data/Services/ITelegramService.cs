namespace Trading.AlertSystem.Data.Services;

/// <summary>
/// Telegram消息服务接口
/// </summary>
public interface ITelegramService
{
    /// <summary>
    /// 发送文本消息
    /// </summary>
    Task<bool> SendMessageAsync(string message, long? chatId = null);

    /// <summary>
    /// 发送带格式的消息（支持Markdown或HTML）
    /// </summary>
    Task<bool> SendFormattedMessageAsync(string message, long? chatId = null, string parseMode = "Markdown");

    /// <summary>
    /// 测试连接
    /// </summary>
    Task<bool> TestConnectionAsync();
}
