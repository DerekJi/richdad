namespace Trading.Infras.Data.Models;

/// <summary>
/// Telegram按钮配置
/// </summary>
public class TelegramButton
{
    /// <summary>
    /// 按钮显示文本
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 按钮回调数据（用户点击后返回的数据）
    /// </summary>
    public string CallbackData { get; set; } = string.Empty;

    /// <summary>
    /// 按钮URL（可选，如果设置了URL则会打开链接而不是触发回调）
    /// </summary>
    public string? Url { get; set; }

    public TelegramButton()
    {
    }

    public TelegramButton(string text, string callbackData)
    {
        Text = text;
        CallbackData = callbackData;
    }

    public TelegramButton(string text, string callbackData, string url)
    {
        Text = text;
        CallbackData = callbackData;
        Url = url;
    }
}

/// <summary>
/// Telegram按钮行（一行可以包含多个按钮）
/// </summary>
public class TelegramButtonRow
{
    public List<TelegramButton> Buttons { get; set; } = new();

    public TelegramButtonRow()
    {
    }

    public TelegramButtonRow(params TelegramButton[] buttons)
    {
        Buttons = buttons.ToList();
    }

    public TelegramButtonRow AddButton(string text, string callbackData)
    {
        Buttons.Add(new TelegramButton(text, callbackData));
        return this;
    }
}

/// <summary>
/// Telegram回调查询事件参数
/// </summary>
public class TelegramCallbackQueryEventArgs : EventArgs
{
    /// <summary>
    /// 回调查询ID（用于回复确认）
    /// </summary>
    public string CallbackQueryId { get; set; } = string.Empty;

    /// <summary>
    /// 用户点击的回调数据
    /// </summary>
    public string CallbackData { get; set; } = string.Empty;

    /// <summary>
    /// 消息ID（按钮所在的消息）
    /// </summary>
    public int MessageId { get; set; }

    /// <summary>
    /// Chat ID
    /// </summary>
    public long ChatId { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// 消息文本（按钮所在的消息内容）
    /// </summary>
    public string? MessageText { get; set; }
}
