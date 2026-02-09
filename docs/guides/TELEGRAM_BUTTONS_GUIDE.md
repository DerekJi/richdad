# Telegram 交互式按钮使用指南

## 概述

此功能扩展了Telegram Bot的能力，允许发送带有交互按钮的消息，并接收用户的选择。

## 核心功能

### 1. 发送带按钮的消息

发送带有交互按钮的消息，用户点击后可以收到回调。

```csharp
// 创建按钮
var buttonRows = new List<TelegramButtonRow>
{
    // 第一行：Yes/No 按钮
    new TelegramButtonRow(
        new TelegramButton("✅ Yes", "action:open_position:yes"),
        new TelegramButton("❌ No", "action:open_position:no")
    ),

    // 第二行：三个方案选择
    new TelegramButtonRow(
        new TelegramButton("方案1", "plan:1"),
        new TelegramButton("方案2", "plan:2"),
        new TelegramButton("方案3", "plan:3")
    )
};

// 发送消息
await _telegramService.SendMessageWithButtonsAsync(
    "是否开仓？",
    buttonRows,
    chatId: 123456789
);
```

### 2. 接收用户点击回调

订阅 `OnCallbackQueryReceived` 事件来处理用户的按钮点击。

```csharp
// 在服务初始化时订阅事件
_telegramService.OnCallbackQueryReceived += OnTelegramButtonClicked;

// 启动更新监听（这会启动后台长轮询）
_telegramService.StartReceivingUpdates();

// 事件处理方法
private async void OnTelegramButtonClicked(object? sender, TelegramCallbackQueryEventArgs e)
{
    _logger.LogInformation("用户点击了按钮: {CallbackData}", e.CallbackData);

    // 根据回调数据进行处理
    if (e.CallbackData.StartsWith("action:open_position:"))
    {
        var choice = e.CallbackData.Split(':')[2]; // yes 或 no

        if (choice == "yes")
        {
            // 执行开仓操作
            await OpenPosition();

            // 显示确认提示
            await _telegramService.AnswerCallbackQueryAsync(
                e.CallbackQueryId,
                "✅ 已开仓",
                showAlert: true
            );

            // 更新消息文本，移除按钮
            await _telegramService.EditMessageTextAsync(
                e.ChatId,
                e.MessageId,
                "✅ 已确认开仓"
            );
        }
        else
        {
            await _telegramService.AnswerCallbackQueryAsync(
                e.CallbackQueryId,
                "❌ 已取消"
            );

            await _telegramService.EditMessageTextAsync(
                e.ChatId,
                e.MessageId,
                "❌ 已取消开仓"
            );
        }
    }
}

// 应用关闭时停止监听
public void Dispose()
{
    _telegramService.StopReceivingUpdates();
}
```

### 3. 编辑消息和按钮

可以在用户点击后更新消息内容或按钮。

```csharp
// 只更新按钮
await _telegramService.EditMessageButtonsAsync(
    chatId: 123456789,
    messageId: 12345,
    buttonRows: newButtonRows
);

// 更新文本和按钮
await _telegramService.EditMessageTextAsync(
    chatId: 123456789,
    messageId: 12345,
    newText: "新的消息内容",
    buttonRows: newButtonRows  // 可选
);
```

## 实际使用场景

### 场景1：交易确认

```csharp
public async Task SendTradeConfirmation(string symbol, decimal price, string direction)
{
    var message = $@"
🔔 *交易信号*
品种: {symbol}
价格: {price}
方向: {direction}

是否执行？
";

    var buttons = new List<TelegramButtonRow>
    {
        new TelegramButtonRow(
            new TelegramButton("✅ 执行", $"trade:execute:{symbol}:{price}:{direction}"),
            new TelegramButton("❌ 取消", "trade:cancel")
        )
    };

    await _telegramService.SendMessageWithButtonsAsync(message, buttons);
}
```

### 场景2：多个开仓方案选择

```csharp
public async Task SendPositionOptions(List<PositionPlan> plans)
{
    var message = "*请选择开仓方案*\n\n";

    for (int i = 0; i < plans.Count; i++)
    {
        var plan = plans[i];
        message += $"方案{i + 1}: 手数={plan.Volume}, SL={plan.StopLoss}, TP={plan.TakeProfit}\n";
    }

    var buttonRow = new TelegramButtonRow();
    for (int i = 0; i < plans.Count; i++)
    {
        buttonRow.AddButton($"方案 {i + 1}", $"plan:select:{i}");
    }

    var buttons = new List<TelegramButtonRow> { buttonRow };

    await _telegramService.SendMessageWithButtonsAsync(message, buttons);
}

// 处理方案选择
private async void OnPlanSelected(object? sender, TelegramCallbackQueryEventArgs e)
{
    if (e.CallbackData.StartsWith("plan:select:"))
    {
        var planIndex = int.Parse(e.CallbackData.Split(':')[2]);
        var plan = _availablePlans[planIndex];

        // 执行开仓
        await ExecutePosition(plan);

        // 显示确认
        await _telegramService.AnswerCallbackQueryAsync(
            e.CallbackQueryId,
            $"✅ 已选择方案 {planIndex + 1}",
            showAlert: true
        );

        // 更新消息
        await _telegramService.EditMessageTextAsync(
            e.ChatId,
            e.MessageId,
            $"✅ 已执行方案 {planIndex + 1}\n手数={plan.Volume}, SL={plan.StopLoss}, TP={plan.TakeProfit}"
        );
    }
}
```

### 场景3：分步确认流程

```csharp
// 第一步：发送初始确认
var step1Buttons = new List<TelegramButtonRow>
{
    new TelegramButtonRow(
        new TelegramButton("继续", "flow:step2"),
        new TelegramButton("取消", "flow:cancel")
    )
};
await _telegramService.SendMessageWithButtonsAsync("步骤1：确认开仓？", step1Buttons);

// 在回调中处理
private async void OnFlowButtonClicked(object? sender, TelegramCallbackQueryEventArgs e)
{
    if (e.CallbackData == "flow:step2")
    {
        // 第二步：选择杠杆
        var step2Buttons = new List<TelegramButtonRow>
        {
            new TelegramButtonRow(
                new TelegramButton("10x", "leverage:10"),
                new TelegramButton("20x", "leverage:20"),
                new TelegramButton("50x", "leverage:50")
            )
        };

        await _telegramService.AnswerCallbackQueryAsync(e.CallbackQueryId);
        await _telegramService.EditMessageTextAsync(
            e.ChatId,
            e.MessageId,
            "步骤2：选择杠杆倍数",
            step2Buttons
        );
    }
    else if (e.CallbackData.StartsWith("leverage:"))
    {
        var leverage = e.CallbackData.Split(':')[1];

        // 执行最终操作
        await ExecuteTradeWithLeverage(int.Parse(leverage));

        await _telegramService.AnswerCallbackQueryAsync(
            e.CallbackQueryId,
            $"✅ 已设置 {leverage}x 杠杆并开仓",
            showAlert: true
        );

        await _telegramService.EditMessageTextAsync(
            e.ChatId,
            e.MessageId,
            $"✅ 交易已执行（{leverage}x 杠杆）"
        );
    }
}
```

## 回调数据格式建议

为了便于解析，建议使用以下格式：

```
action:subaction:param1:param2
```

示例：
- `trade:execute:BTCUSDT:50000:long`
- `plan:select:2`
- `confirm:yes`
- `cancel`

## 注意事项

1. **回调数据限制**：Telegram对回调数据有64字节的长度限制，建议使用简短的标识符
2. **必须回复**：收到回调查询后必须调用 `AnswerCallbackQueryAsync`，否则用户界面会显示加载状态
3. **长轮询开销**：`StartReceivingUpdates()` 会启动后台长轮询，需要在应用关闭时调用 `StopReceivingUpdates()`
4. **线程安全**：回调事件可能在不同的线程中触发，需要注意线程安全
5. **演示模式**：在演示模式（DemoTelegramService）下，按钮功能只会记录日志，不会实际发送

## 集成到现有服务

在你的服务类中：

```csharp
public class TradingAlertService
{
    private readonly ITelegramService _telegramService;

    public TradingAlertService(ITelegramService telegramService)
    {
        _telegramService = telegramService;

        // 订阅按钮点击事件
        _telegramService.OnCallbackQueryReceived += HandleTelegramCallback;

        // 启动更新监听
        _telegramService.StartReceivingUpdates();
    }

    private async void HandleTelegramCallback(object? sender, TelegramCallbackQueryEventArgs e)
    {
        // 处理所有按钮点击
        try
        {
            // 根据回调数据路由到不同的处理器
            if (e.CallbackData.StartsWith("trade:"))
            {
                await HandleTradeCallback(e);
            }
            else if (e.CallbackData.StartsWith("plan:"))
            {
                await HandlePlanCallback(e);
            }
            // ... 其他处理
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理Telegram回调时出错");
            await _telegramService.AnswerCallbackQueryAsync(
                e.CallbackQueryId,
                "❌ 处理失败，请重试",
                showAlert: true
            );
        }
    }
}
```

## 最佳实践

1. **使用结构化的回调数据**：便于解析和维护
2. **总是回复回调查询**：提供良好的用户体验
3. **更新消息状态**：点击后更新消息文本，避免重复点击
4. **错误处理**：捕获异常并给用户反馈
5. **清理资源**：应用关闭时停止更新监听
