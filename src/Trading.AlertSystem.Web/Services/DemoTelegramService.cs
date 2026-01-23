using Microsoft.Extensions.Logging;
using Trading.AlertSystem.Data.Services;

namespace Trading.AlertSystem.Web.Services;

/// <summary>
/// 演示模式的Telegram服务（不实际发送消息，仅记录日志）
/// </summary>
public class DemoTelegramService : ITelegramService
{
    private readonly ILogger<DemoTelegramService> _logger;

    public DemoTelegramService(ILogger<DemoTelegramService> logger)
    {
        _logger = logger;
        _logger.LogWarning("使用演示模式Telegram服务 - 消息仅记录日志，不会实际发送。请配置真实的Bot Token以启用实际发送功能。");
    }

    public Task<bool> SendMessageAsync(string message, long? chatId = null)
    {
        _logger.LogInformation("[演示模式] Telegram消息: {Message}, Chat ID: {ChatId}", message, chatId ?? 0);
        return Task.FromResult(true);
    }

    public Task<bool> SendFormattedMessageAsync(string message, long? chatId = null, string parseMode = "Markdown")
    {
        _logger.LogInformation("[演示模式] Telegram格式化消息 ({ParseMode}): {Message}, Chat ID: {ChatId}",
            parseMode, message, chatId ?? 0);
        return Task.FromResult(true);
    }

    public Task<bool> TestConnectionAsync()
    {
        _logger.LogInformation("[演示模式] Telegram连接测试 - 演示模式总是返回成功");
        return Task.FromResult(true);
    }
}
