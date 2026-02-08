using Trading.AlertSystem.Data.Models;

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
    /// 发送图片消息
    /// </summary>
    Task<bool> SendPhotoAsync(Stream photoStream, string? caption = null, long? chatId = null);

    /// <summary>
    /// 发送带交互按钮的消息
    /// </summary>
    /// <param name="message">消息文本</param>
    /// <param name="buttonRows">按钮行列表（每行可以有多个按钮）</param>
    /// <param name="chatId">Chat ID</param>
    /// <param name="parseMode">解析模式（Markdown或HTML）</param>
    /// <returns>是否发送成功</returns>
    Task<bool> SendMessageWithButtonsAsync(
        string message,
        List<TelegramButtonRow> buttonRows,
        long? chatId = null,
        string parseMode = "Markdown");

    /// <summary>
    /// 回复回调查询（当用户点击按钮后必须调用此方法确认）
    /// </summary>
    /// <param name="callbackQueryId">回调查询ID</param>
    /// <param name="text">显示给用户的提示文本（可选）</param>
    /// <param name="showAlert">是否显示为弹窗（true）还是顶部通知（false）</param>
    /// <returns>是否回复成功</returns>
    Task<bool> AnswerCallbackQueryAsync(string callbackQueryId, string? text = null, bool showAlert = false);

    /// <summary>
    /// 编辑消息的按钮（保持消息文本不变，只更新按钮）
    /// </summary>
    /// <param name="chatId">Chat ID</param>
    /// <param name="messageId">消息ID</param>
    /// <param name="buttonRows">新的按钮行列表</param>
    /// <returns>是否编辑成功</returns>
    Task<bool> EditMessageButtonsAsync(long chatId, int messageId, List<TelegramButtonRow> buttonRows);

    /// <summary>
    /// 编辑消息文本和按钮
    /// </summary>
    /// <param name="chatId">Chat ID</param>
    /// <param name="messageId">消息ID</param>
    /// <param name="newText">新的消息文本</param>
    /// <param name="buttonRows">新的按钮行列表（可选）</param>
    /// <param name="parseMode">解析模式（Markdown或HTML）</param>
    /// <returns>是否编辑成功</returns>
    Task<bool> EditMessageTextAsync(
        long chatId,
        int messageId,
        string newText,
        List<TelegramButtonRow>? buttonRows = null,
        string parseMode = "Markdown");

    /// <summary>
    /// 启动Bot的更新监听（用于接收用户的按钮点击等事件）
    /// 注意：这会启动一个后台任务来接收更新
    /// </summary>
    void StartReceivingUpdates();

    /// <summary>
    /// 停止Bot的更新监听
    /// </summary>
    void StopReceivingUpdates();

    /// <summary>
    /// 测试连接
    /// </summary>
    Task<bool> TestConnectionAsync();

    /// <summary>
    /// 当用户点击按钮时触发的事件
    /// </summary>
    event EventHandler<TelegramCallbackQueryEventArgs>? OnCallbackQueryReceived;
}
