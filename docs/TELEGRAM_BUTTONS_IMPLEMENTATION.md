# Telegram 交互式按钮功能 - 实现总结

## 已完成的改动

### 1. 新增的文件

#### 模型类
- **`Trading.AlertSystem.Data/Models/TelegramButton.cs`**
  - `TelegramButton`: 按钮配置类（文本、回调数据、URL）
  - `TelegramButtonRow`: 按钮行（一行可包含多个按钮）
  - `TelegramCallbackQueryEventArgs`: 回调查询事件参数

#### 服务示例
- **`Trading.AlertSystem.Service/Services/TelegramInteractiveService.cs`**
  - 完整的交互式服务实现示例
  - 包含交易确认、方案选择等场景
  - 展示了事件订阅和处理的最佳实践

#### 文档
- **`docs/TELEGRAM_BUTTONS_GUIDE.md`**
  - 详细的使用指南
  - 多个实际使用场景示例
  - 最佳实践和注意事项

### 2. 修改的文件

#### 接口扩展
- **`Trading.AlertSystem.Data/Services/ITelegramService.cs`**
  - 新增方法：
    - `SendMessageWithButtonsAsync`: 发送带按钮的消息
    - `AnswerCallbackQueryAsync`: 回复按钮点击
    - `EditMessageButtonsAsync`: 编辑消息按钮
    - `EditMessageTextAsync`: 编辑消息文本和按钮
    - `StartReceivingUpdates`: 启动更新监听
    - `StopReceivingUpdates`: 停止更新监听
  - 新增事件：
    - `OnCallbackQueryReceived`: 按钮点击事件

#### 服务实现
- **`Trading.AlertSystem.Data/Services/TelegramService.cs`**
  - 实现了所有新增的接口方法
  - 添加了长轮询机制来接收用户的按钮点击
  - 包含完整的错误处理和日志记录

- **`Trading.AlertSystem.Web/Services/DemoTelegramService.cs`**
  - 实现了所有新接口方法（演示模式）
  - 只记录日志，不实际发送

## 核心功能

### 1. 发送交互式消息

```csharp
var buttons = new List<TelegramButtonRow>
{
    new TelegramButtonRow(
        new TelegramButton("✅ Yes", "action:yes"),
        new TelegramButton("❌ No", "action:no")
    )
};

await telegramService.SendMessageWithButtonsAsync("是否开仓？", buttons);
```

### 2. 接收用户点击

```csharp
// 订阅事件
telegramService.OnCallbackQueryReceived += async (sender, e) =>
{
    Console.WriteLine($"用户点击了: {e.CallbackData}");

    // 必须回复确认
    await telegramService.AnswerCallbackQueryAsync(
        e.CallbackQueryId,
        "✅ 已确认"
    );
};

// 启动监听
telegramService.StartReceivingUpdates();
```

### 3. 更新消息

```csharp
// 用户点击后更新消息
await telegramService.EditMessageTextAsync(
    chatId: e.ChatId,
    messageId: e.MessageId,
    newText: "✅ 操作已完成"
);
```

## 工作原理

1. **长轮询机制**: `StartReceivingUpdates()` 启动后台任务，持续监听Telegram服务器的更新
2. **回调数据**: 每个按钮携带一个回调字符串（最多64字节），点击时返回
3. **事件驱动**: 用户点击按钮时触发 `OnCallbackQueryReceived` 事件
4. **消息更新**: 可以动态更新消息内容和按钮，提供流畅的交互体验

## 使用步骤

### 步骤1：注册服务
```csharp
services.AddSingleton<ITelegramService, TelegramService>();
services.AddSingleton<TelegramInteractiveService>();
```

### 步骤2：订阅事件
```csharp
telegramService.OnCallbackQueryReceived += HandleButtonClick;
telegramService.StartReceivingUpdates();
```

### 步骤3：发送带按钮的消息
```csharp
var buttons = new List<TelegramButtonRow>
{
    new TelegramButtonRow(
        new TelegramButton("选项1", "option:1"),
        new TelegramButton("选项2", "option:2")
    )
};

await telegramService.SendMessageWithButtonsAsync("请选择：", buttons);
```

### 步骤4：处理回调
```csharp
private async void HandleButtonClick(object? sender, TelegramCallbackQueryEventArgs e)
{
    if (e.CallbackData == "option:1")
    {
        // 处理选项1
        await DoSomething();

        // 回复确认
        await telegramService.AnswerCallbackQueryAsync(
            e.CallbackQueryId,
            "✅ 已选择选项1"
        );

        // 更新消息
        await telegramService.EditMessageTextAsync(
            e.ChatId,
            e.MessageId,
            "✅ 已完成"
        );
    }
}
```

### 步骤5：应用关闭时清理
```csharp
public void Dispose()
{
    telegramService.StopReceivingUpdates();
}
```

## 实际应用场景

### 1. 交易确认
发送交易信号时附带"执行"/"取消"按钮，用户点击后执行相应操作。

### 2. 方案选择
提供多个开仓方案（不同手数、止损、止盈），用户选择后执行。

### 3. 分步流程
实现多步确认流程，每步更新消息和按钮。

### 4. 快速响应
用户可以直接在Telegram中做出决策，无需打开其他应用。

## 技术细节

### 回调数据格式
建议使用结构化格式：
```
action:subaction:param1:param2
```

示例：
- `trade:execute:BTCUSDT:50000:long`
- `plan:select:2`
- `confirm:yes`

### 限制和注意事项

1. **回调数据长度**: 最多64字节
2. **必须回复**: 收到回调后必须调用 `AnswerCallbackQueryAsync`
3. **线程安全**: 事件可能在不同线程触发
4. **资源清理**: 应用关闭时调用 `StopReceivingUpdates()`
5. **演示模式**: DemoTelegramService 只记录日志

### 依赖的NuGet包
- `Telegram.Bot` (版本 22.0.0) - 已安装，无需额外操作

## 下一步

要在应用中使用交互式按钮：

1. 查看 **`docs/TELEGRAM_BUTTONS_GUIDE.md`** 了解详细用法
2. 参考 **`TelegramInteractiveService.cs`** 的实现示例
3. 在你的服务中订阅 `OnCallbackQueryReceived` 事件
4. 调用 `StartReceivingUpdates()` 启动监听
5. 使用 `SendMessageWithButtonsAsync()` 发送交互式消息

## 测试建议

### 本地测试
1. 配置真实的 Bot Token 和 Chat ID
2. 启动应用
3. 发送带按钮的测试消息
4. 在Telegram中点击按钮
5. 查看日志确认回调被正确处理

### 演示模式测试
如果使用 `DemoTelegramService`（Bot Token 为空时自动使用）：
- 所有操作只记录日志
- 不会实际发送消息或接收回调
- 适合开发阶段的功能测试

## 总结

所有Infrastructure层的改动已完成：
- ✅ 模型类（TelegramButton, TelegramButtonRow, TelegramCallbackQueryEventArgs）
- ✅ 接口扩展（ITelegramService）
- ✅ 服务实现（TelegramService, DemoTelegramService）
- ✅ 长轮询机制（StartReceivingUpdates/StopReceivingUpdates）
- ✅ 事件系统（OnCallbackQueryReceived）
- ✅ 示例服务（TelegramInteractiveService）
- ✅ 完整文档（TELEGRAM_BUTTONS_GUIDE.md）

现在可以在应用层使用这些功能实现交互式的交易确认和方案选择！
