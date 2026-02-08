# Telegram 交互式按钮 - 快速开始

## 最简单的示例

### 1. 发送一个带Yes/No按钮的消息

```csharp
// 注入服务
private readonly ITelegramService _telegramService;

// 发送消息
public async Task SendConfirmation()
{
    var buttons = new List<TelegramButtonRow>
    {
        new TelegramButtonRow(
            new TelegramButton("✅ Yes", "confirm:yes"),
            new TelegramButton("❌ No", "confirm:no")
        )
    };

    await _telegramService.SendMessageWithButtonsAsync(
        "是否执行操作？",
        buttons
    );
}
```

### 2. 接收用户的选择

```csharp
// 在构造函数或初始化方法中
public void Initialize()
{
    // 订阅事件
    _telegramService.OnCallbackQueryReceived += OnUserClicked;

    // 启动监听
    _telegramService.StartReceivingUpdates();
}

// 处理点击
private async void OnUserClicked(object? sender, TelegramCallbackQueryEventArgs e)
{
    if (e.CallbackData == "confirm:yes")
    {
        // 用户点击了Yes
        await _telegramService.AnswerCallbackQueryAsync(
            e.CallbackQueryId,
            "✅ 已确认"
        );

        // 执行你的业务逻辑
        await DoSomething();
    }
    else if (e.CallbackData == "confirm:no")
    {
        // 用户点击了No
        await _telegramService.AnswerCallbackQueryAsync(
            e.CallbackQueryId,
            "❌ 已取消"
        );
    }
}
```

## 完整示例：交易确认流程

```csharp
using Trading.AlertSystem.Data.Models;
using Trading.AlertSystem.Data.Services;

public class TradingService
{
    private readonly ITelegramService _telegramService;
    private readonly ILogger<TradingService> _logger;

    public TradingService(
        ITelegramService telegramService,
        ILogger<TradingService> logger)
    {
        _telegramService = telegramService;
        _logger = logger;

        // 启动时订阅事件
        _telegramService.OnCallbackQueryReceived += HandleButtonClick;
        _telegramService.StartReceivingUpdates();
    }

    // 发送交易信号
    public async Task SendTradeSignal(string symbol, decimal price)
    {
        var message = $@"
🔔 *新交易信号*
品种: {symbol}
价格: {price}

是否执行？
";

        var buttons = new List<TelegramButtonRow>
        {
            new TelegramButtonRow(
                new TelegramButton("✅ 执行", $"trade:{symbol}:{price}"),
                new TelegramButton("❌ 取消", "trade:cancel")
            )
        };

        await _telegramService.SendMessageWithButtonsAsync(message, buttons);
    }

    // 处理按钮点击
    private async void HandleButtonClick(object? sender, TelegramCallbackQueryEventArgs e)
    {
        try
        {
            if (e.CallbackData.StartsWith("trade:") && e.CallbackData != "trade:cancel")
            {
                // 解析数据
                var parts = e.CallbackData.Split(':');
                var symbol = parts[1];
                var price = decimal.Parse(parts[2]);

                // 执行交易
                await ExecuteTrade(symbol, price);

                // 显示确认
                await _telegramService.AnswerCallbackQueryAsync(
                    e.CallbackQueryId,
                    $"✅ {symbol} 交易已执行",
                    showAlert: true
                );

                // 更新消息
                await _telegramService.EditMessageTextAsync(
                    e.ChatId,
                    e.MessageId,
                    $"✅ {symbol} @ {price} - 交易已执行"
                );
            }
            else if (e.CallbackData == "trade:cancel")
            {
                await _telegramService.AnswerCallbackQueryAsync(
                    e.CallbackQueryId,
                    "❌ 已取消"
                );

                await _telegramService.EditMessageTextAsync(
                    e.ChatId,
                    e.MessageId,
                    "❌ 交易已取消"
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理按钮点击失败");
            await _telegramService.AnswerCallbackQueryAsync(
                e.CallbackQueryId,
                "❌ 处理失败",
                showAlert: true
            );
        }
    }

    private async Task ExecuteTrade(string symbol, decimal price)
    {
        // 你的交易逻辑
        _logger.LogInformation("执行交易: {Symbol} @ {Price}", symbol, price);
        await Task.Delay(100); // 模拟执行
    }

    // 清理资源
    public void Dispose()
    {
        _telegramService.StopReceivingUpdates();
    }
}
```

## 3个方案选择示例

```csharp
public async Task SendPlanSelection()
{
    var message = @"
*选择开仓方案*

方案1: 保守 (手数=0.1, SL=50)
方案2: 标准 (手数=0.5, SL=30)
方案3: 激进 (手数=1.0, SL=20)
";

    var buttons = new List<TelegramButtonRow>
    {
        new TelegramButtonRow(
            new TelegramButton("方案1", "plan:1"),
            new TelegramButton("方案2", "plan:2"),
            new TelegramButton("方案3", "plan:3")
        )
    };

    await _telegramService.SendMessageWithButtonsAsync(message, buttons);
}

// 在HandleButtonClick中添加：
if (e.CallbackData.StartsWith("plan:"))
{
    var planNumber = e.CallbackData.Split(':')[1];

    await _telegramService.AnswerCallbackQueryAsync(
        e.CallbackQueryId,
        $"✅ 已选择方案{planNumber}"
    );

    await _telegramService.EditMessageTextAsync(
        e.ChatId,
        e.MessageId,
        $"✅ 方案{planNumber}已执行"
    );

    // 执行方案
    await ExecutePlan(int.Parse(planNumber));
}
```

## 常见问题

### Q: 必须调用AnswerCallbackQueryAsync吗？
**A:** 是的！如果不调用，用户界面会一直显示"加载中"状态。

### Q: 回调数据有长度限制吗？
**A:** 是的，最多64字节。建议使用简短的标识符。

### Q: 如何停止接收更新？
**A:** 在应用关闭时调用 `_telegramService.StopReceivingUpdates()`

### Q: 演示模式下会工作吗？
**A:** 演示模式（DemoTelegramService）只会记录日志，不会实际发送消息或接收回调。

## 下一步

- 查看 [详细使用指南](TELEGRAM_BUTTONS_GUIDE.md)
- 查看 [实现总结](TELEGRAM_BUTTONS_IMPLEMENTATION.md)
- 查看示例服务实现: `Trading.AlertSystem.Service/Services/TelegramInteractiveService.cs`

## 提示

1. **结构化的回调数据**: 使用 `action:param1:param2` 格式
2. **错误处理**: 总是用try-catch包裹回调处理代码
3. **更新消息**: 点击后更新消息状态，避免重复点击
4. **资源清理**: 应用关闭时停止更新监听
