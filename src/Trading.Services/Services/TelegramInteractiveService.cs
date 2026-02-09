using Microsoft.Extensions.Logging;
using Trading.Infrastructure.Models;
using Trading.Infrastructure.Services;

namespace Trading.Services.Services;

/// <summary>
/// Telegram交互式消息服务示例
/// 展示如何使用按钮功能实现交互式交易确认
/// </summary>
public class TelegramInteractiveService : IDisposable
{
    private readonly ITelegramService _telegramService;
    private readonly ILogger<TelegramInteractiveService> _logger;
    private readonly Dictionary<string, object> _pendingActions = new();

    public TelegramInteractiveService(
        ITelegramService telegramService,
        ILogger<TelegramInteractiveService> logger)
    {
        _telegramService = telegramService;
        _logger = logger;

        // 订阅按钮点击事件
        _telegramService.OnCallbackQueryReceived += OnButtonClicked;

        // 启动更新监听
        _telegramService.StartReceivingUpdates();
        _logger.LogInformation("Telegram交互式服务已启动");
    }

    /// <summary>
    /// 发送交易确认消息（Yes/No按钮）
    /// </summary>
    public async Task SendTradeConfirmationAsync(string symbol, decimal price, string direction, long? chatId = null)
    {
        var actionId = Guid.NewGuid().ToString("N")[..8]; // 生成短ID

        // 保存待处理的交易信息
        _pendingActions[actionId] = new
        {
            Type = "trade",
            Symbol = symbol,
            Price = price,
            Direction = direction,
            Timestamp = DateTime.UtcNow
        };

        var message = $@"
🔔 *交易信号*
📊 品种: `{symbol}`
💰 价格: `{price}`
📈 方向: `{direction}`

是否执行交易？
";

        var buttons = new List<TelegramButtonRow>
        {
            new TelegramButtonRow(
                new TelegramButton("✅ 执行", $"confirm:{actionId}"),
                new TelegramButton("❌ 取消", $"cancel:{actionId}")
            )
        };

        await _telegramService.SendMessageWithButtonsAsync(message, buttons, chatId);
        _logger.LogInformation("已发送交易确认: {Symbol} @ {Price} ({Direction})", symbol, price, direction);
    }

    /// <summary>
    /// 发送多方案选择消息
    /// </summary>
    public async Task SendPositionPlansAsync(string symbol, List<PositionPlan> plans, long? chatId = null)
    {
        var actionId = Guid.NewGuid().ToString("N")[..8];

        _pendingActions[actionId] = new
        {
            Type = "plan",
            Symbol = symbol,
            Plans = plans,
            Timestamp = DateTime.UtcNow
        };

        var message = $@"
📊 *{symbol} 开仓方案*

";
        for (int i = 0; i < plans.Count; i++)
        {
            var plan = plans[i];
            message += $@"
*方案 {i + 1}*
• 手数: `{plan.Volume}`
• 止损: `{plan.StopLoss}`
• 止盈: `{plan.TakeProfit}`
• 风险率: `{plan.RiskPercent:F2}%`

";
        }

        message += "请选择一个方案：";

        // 创建按钮行（每行最多3个按钮）
        var buttonRows = new List<TelegramButtonRow>();
        for (int i = 0; i < plans.Count; i += 3)
        {
            var row = new TelegramButtonRow();
            for (int j = i; j < Math.Min(i + 3, plans.Count); j++)
            {
                row.AddButton($"方案 {j + 1}", $"plan:{actionId}:{j}");
            }
            buttonRows.Add(row);
        }

        // 添加取消按钮
        buttonRows.Add(new TelegramButtonRow(
            new TelegramButton("❌ 取消", $"cancel:{actionId}")
        ));

        await _telegramService.SendMessageWithButtonsAsync(message, buttonRows, chatId, "Markdown");
        _logger.LogInformation("已发送方案选择: {Symbol}, {Count}个方案", symbol, plans.Count);
    }

    /// <summary>
    /// 处理按钮点击事件
    /// </summary>
    private async void OnButtonClicked(object? sender, TelegramCallbackQueryEventArgs e)
    {
        try
        {
            _logger.LogInformation("收到按钮点击: {Data} from {User}", e.CallbackData, e.Username ?? e.UserId.ToString());

            var parts = e.CallbackData.Split(':');
            var action = parts[0];
            var actionId = parts.Length > 1 ? parts[1] : null;

            switch (action)
            {
                case "confirm":
                    await HandleConfirmAction(e, actionId!);
                    break;

                case "cancel":
                    await HandleCancelAction(e, actionId!);
                    break;

                case "plan":
                    var planIndex = int.Parse(parts[2]);
                    await HandlePlanSelection(e, actionId!, planIndex);
                    break;

                default:
                    _logger.LogWarning("未知的按钮操作: {Action}", action);
                    await _telegramService.AnswerCallbackQueryAsync(e.CallbackQueryId, "❌ 未知操作");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理按钮点击时出错: {Data}", e.CallbackData);
            await _telegramService.AnswerCallbackQueryAsync(
                e.CallbackQueryId,
                "❌ 处理失败，请重试",
                showAlert: true
            );
        }
    }

    /// <summary>
    /// 处理确认操作
    /// </summary>
    private async Task HandleConfirmAction(TelegramCallbackQueryEventArgs e, string actionId)
    {
        if (!_pendingActions.TryGetValue(actionId, out var actionData))
        {
            await _telegramService.AnswerCallbackQueryAsync(e.CallbackQueryId, "❌ 操作已过期");
            return;
        }

        dynamic data = actionData;
        string symbol = data.Symbol;
        decimal price = data.Price;
        string direction = data.Direction;

        // 这里执行实际的交易逻辑
        _logger.LogInformation("执行交易: {Symbol} @ {Price} ({Direction})",
            symbol, price, direction);

        // 显示确认提示
        await _telegramService.AnswerCallbackQueryAsync(
            e.CallbackQueryId,
            $"✅ 交易已执行: {data.Symbol}",
            showAlert: true
        );

        // 更新消息
        var updatedMessage = $@"
✅ *交易已执行*
📊 品种: `{data.Symbol}`
💰 价格: `{data.Price}`
📈 方向: `{data.Direction}`
⏰ 时间: `{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC`
";

        await _telegramService.EditMessageTextAsync(
            e.ChatId,
            e.MessageId,
            updatedMessage
        );

        // 清理待处理操作
        _pendingActions.Remove(actionId);
    }

    /// <summary>
    /// 处理取消操作
    /// </summary>
    private async Task HandleCancelAction(TelegramCallbackQueryEventArgs e, string actionId)
    {
        _pendingActions.Remove(actionId);

        await _telegramService.AnswerCallbackQueryAsync(e.CallbackQueryId, "❌ 已取消");

        await _telegramService.EditMessageTextAsync(
            e.ChatId,
            e.MessageId,
            "❌ 操作已取消"
        );

        _logger.LogInformation("用户取消了操作: {ActionId}", actionId);
    }

    /// <summary>
    /// 处理方案选择
    /// </summary>
    private async Task HandlePlanSelection(TelegramCallbackQueryEventArgs e, string actionId, int planIndex)
    {
        if (!_pendingActions.TryGetValue(actionId, out var actionData))
        {
            await _telegramService.AnswerCallbackQueryAsync(e.CallbackQueryId, "❌ 操作已过期");
            return;
        }

        dynamic data = actionData;
        string symbol = data.Symbol;
        var plans = (List<PositionPlan>)data.Plans;

        if (planIndex < 0 || planIndex >= plans.Count)
        {
            await _telegramService.AnswerCallbackQueryAsync(e.CallbackQueryId, "✖️ 无效的方案");
            return;
        }

        var selectedPlan = plans[planIndex];

        // 这里执行实际的开仓逻辑
        _logger.LogInformation("执行方案: {Symbol}, 方案{Index}, 手数={Volume}",
            symbol, planIndex + 1, selectedPlan.Volume);

        await _telegramService.AnswerCallbackQueryAsync(
            e.CallbackQueryId,
            $"✅ 已选择方案 {planIndex + 1}",
            showAlert: true
        );

        // 更新消息
        var updatedMessage = $@"
✅ *方案已执行*
📊 品种: `{data.Symbol}`
📋 方案: `方案 {planIndex + 1}`
💼 手数: `{selectedPlan.Volume}`
🛑 止损: `{selectedPlan.StopLoss}`
🎯 止盈: `{selectedPlan.TakeProfit}`
⚠️ 风险率: `{selectedPlan.RiskPercent:F2}%`
⏰ 时间: `{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC`
";

        await _telegramService.EditMessageTextAsync(
            e.ChatId,
            e.MessageId,
            updatedMessage
        );

        _pendingActions.Remove(actionId);
    }

    public void Dispose()
    {
        _telegramService.OnCallbackQueryReceived -= OnButtonClicked;
        _telegramService.StopReceivingUpdates();
        _logger.LogInformation("Telegram交互式服务已停止");
    }
}

/// <summary>
/// 开仓方案模型
/// </summary>
public class PositionPlan
{
    public decimal Volume { get; set; }
    public decimal StopLoss { get; set; }
    public decimal TakeProfit { get; set; }
    public decimal RiskPercent { get; set; }
}
