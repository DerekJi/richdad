using Trading.Infras.Data.Services;
using Trading.Infras.Data.Models;

namespace Trading.Infras.Web.Services;

/// <summary>
/// 演示模式的Telegram服务（不实际发送消息，仅记录日志）
/// </summary>
public class DemoTelegramService : ITelegramService
{
    private readonly ILogger<DemoTelegramService> _logger;

    public event EventHandler<TelegramCallbackQueryEventArgs>? OnCallbackQueryReceived;

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

    public Task<bool> SendPhotoAsync(Stream photoStream, string? caption = null, long? chatId = null)
    {
        _logger.LogInformation("[演示模式] Telegram图片消息: Caption={Caption}, Chat ID: {ChatId}, Stream Length={Length}",
            caption ?? "无", chatId ?? 0, photoStream?.Length ?? 0);
        return Task.FromResult(true);
    }

    public Task<bool> SendMessageWithButtonsAsync(
        string message,
        List<TelegramButtonRow> buttonRows,
        long? chatId = null,
        string parseMode = "Markdown")
    {
        var buttonInfo = string.Join(", ", buttonRows.SelectMany(row =>
            row.Buttons.Select(btn => $"{btn.Text}({btn.CallbackData})")));

        _logger.LogInformation("[演示模式] Telegram带按钮的消息 ({ParseMode}): {Message}, 按钮: [{Buttons}], Chat ID: {ChatId}",
            parseMode, message, buttonInfo, chatId ?? 0);
        return Task.FromResult(true);
    }

    public Task<bool> AnswerCallbackQueryAsync(string callbackQueryId, string? text = null, bool showAlert = false)
    {
        _logger.LogInformation("[演示模式] 回复回调查询: ID={CallbackQueryId}, Text={Text}, ShowAlert={ShowAlert}",
            callbackQueryId, text ?? "无", showAlert);
        return Task.FromResult(true);
    }

    public Task<bool> EditMessageButtonsAsync(long chatId, int messageId, List<TelegramButtonRow> buttonRows)
    {
        var buttonInfo = string.Join(", ", buttonRows.SelectMany(row =>
            row.Buttons.Select(btn => $"{btn.Text}({btn.CallbackData})")));

        _logger.LogInformation("[演示模式] 编辑消息按钮: Chat ID={ChatId}, Message ID={MessageId}, 新按钮: [{Buttons}]",
            chatId, messageId, buttonInfo);
        return Task.FromResult(true);
    }

    public Task<bool> EditMessageTextAsync(
        long chatId,
        int messageId,
        string newText,
        List<TelegramButtonRow>? buttonRows = null,
        string parseMode = "Markdown")
    {
        var buttonInfo = buttonRows != null
            ? string.Join(", ", buttonRows.SelectMany(row => row.Buttons.Select(btn => $"{btn.Text}({btn.CallbackData})")))
            : "无";

        _logger.LogInformation("[演示模式] 编辑消息文本 ({ParseMode}): Chat ID={ChatId}, Message ID={MessageId}, 新文本={Text}, 按钮: [{Buttons}]",
            parseMode, chatId, messageId, newText, buttonInfo);
        return Task.FromResult(true);
    }

    public void StartReceivingUpdates()
    {
        _logger.LogInformation("[演示模式] 启动Bot更新监听 - 演示模式不实际监听");
    }

    public void StopReceivingUpdates()
    {
        _logger.LogInformation("[演示模式] 停止Bot更新监听 - 演示模式不实际监听");
    }

    public Task<bool> TestConnectionAsync()
    {
        _logger.LogInformation("[演示模式] Telegram连接测试 - 演示模式总是返回成功");
        return Task.FromResult(true);
    }
}
