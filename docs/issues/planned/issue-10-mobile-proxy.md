## Issue 10: 实现移动端交易代理（避免 IP 红线）

### 标题
📱 Implement Mobile Trading Proxy to Avoid Prop Firm IP Detection

### 描述
开发轻量级手机 App，接收云端交易信号并在本地执行，避免触发 Prop Firm（如 FTMO）的 EA/VPS IP 检测。

### 背景
许多 Prop Firms 禁止使用 EA（Expert Advisor）或 VPS 进行自动交易：
- **IP 检测**：交易请求来自数据中心 IP 会被标记
- **执行模式检测**：毫秒级响应会被怀疑使用机器人
- **账号封禁风险**：违规使用 EA 可能导致账号冻结

通过移动端代理方案：
- **IP 安全**：交易请求来自手机网络（4G/5G/家庭 WiFi）
- **人工确认**：保留最后的确认步骤，避免完全自动化
- **灵活执行**：可以在任何地点（家、办公室）执行交易
- **FTMO 合规**：满足"手动交易"要求

### 架构设计

```
┌─────────────────────────────────────────────────────────┐
│  Azure Functions (Cloud)                                │
│  ├─ 四级 AI 决策系统                                     │
│  ├─ 生成交易信号（Entry/SL/TP）                          │
│  └─ 推送到 SignalR Hub                                   │
└────────────────────┬────────────────────────────────────┘
                     ↓ (SignalR Real-time Push)
┌─────────────────────────────────────────────────────────┐
│  Mobile App (.NET MAUI)                                 │
│  ├─ 后台服务持续监听信号                                  │
│  ├─ 收到信号后震动提醒                                   │
│  ├─ 显示 AI 推理过程和交易参数                           │
│  └─ 用户点击确认后，调用 OANDA SDK 下单                  │
└─────────────────────────────────────────────────────────┘
                     ↓ (HTTPS from Mobile IP)
┌─────────────────────────────────────────────────────────┐
│  OANDA / TradeLocker API                                │
│  └─ 接收来自手机 IP 的交易请求                           │
└─────────────────────────────────────────────────────────┘
```

### 实现功能

#### ✅ 1. SignalR Hub（云端信号推送）

**新增 Hub：** `TradingSignalHub`

```csharp
public class TradingSignalHub : Hub
{
    private readonly ILogger<TradingSignalHub> _logger;

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation("客户端连接: {UserId}", userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation("客户端断开: {UserId}", userId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// 客户端注册设备
    /// </summary>
    public async Task RegisterDevice(string deviceId, string deviceName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"device_{deviceId}");
        _logger.LogInformation("设备注册: {DeviceId} - {DeviceName}", deviceId, deviceName);
    }
}
```

**信号推送服务：**

```csharp
public class SignalPushService
{
    private readonly IHubContext<TradingSignalHub> _hubContext;
    private readonly ILogger<SignalPushService> _logger;

    /// <summary>
    /// 推送交易信号到移动端
    /// </summary>
    public async Task PushTradingSignalAsync(
        string deviceId,
        TradingSignal signal)
    {
        _logger.LogInformation(
            "推送交易信号到设备 {DeviceId}: {Symbol} {Direction}",
            deviceId, signal.Symbol, signal.Direction);

        await _hubContext.Clients
            .Group($"device_{deviceId}")
            .SendAsync("ReceiveTradingSignal", signal);
    }

    /// <summary>
    /// 推送通用通知
    /// </summary>
    public async Task PushNotificationAsync(
        string deviceId,
        string title,
        string message)
    {
        await _hubContext.Clients
            .Group($"device_{deviceId}")
            .SendAsync("ReceiveNotification", new { title, message });
    }
}
```

**交易信号模型：**

```csharp
public class TradingSignal
{
    public string SignalId { get; set; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 交易参数
    public string Symbol { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty; // Buy/Sell
    public double EntryPrice { get; set; }
    public double StopLoss { get; set; }
    public double TakeProfit { get; set; }
    public double SuggestedLotSize { get; set; }

    // AI 决策上下文
    public string L1_DailyBias { get; set; } = string.Empty;
    public string L2_Structure { get; set; } = string.Empty;
    public string L3_SetupType { get; set; } = string.Empty;
    public string L4_Reasoning { get; set; } = string.Empty;
    public string L4_ThinkingProcess { get; set; } = string.Empty;
    public int ConfidenceScore { get; set; }
    public List<string> RiskFactors { get; set; } = new();

    // 有效期
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}
```

#### ✅ 2. 移动端 App (.NET MAUI)

**项目结构：**

```
TradingMobile/
├── TradingMobile.csproj
├── MauiProgram.cs
├── Services/
│   ├── SignalRService.cs           # SignalR 连接管理
│   ├── BackgroundListenerService.cs # 后台监听服务
│   ├── OandaExecutionService.cs     # OANDA 下单
│   └── NotificationService.cs       # 本地通知
├── ViewModels/
│   ├── MainViewModel.cs
│   ├── SignalDetailsViewModel.cs
│   └── TradeHistoryViewModel.cs
├── Views/
│   ├── MainPage.xaml                # 主界面
│   ├── SignalDetailsPage.xaml      # 信号详情
│   └── SettingsPage.xaml           # 设置页面
└── Models/
    ├── TradingSignal.cs
    └── TradeExecution.cs
```

**SignalRService.cs:**

```csharp
public class SignalRService
{
    private HubConnection? _connection;
    private readonly ILogger<SignalRService> _logger;
    private readonly NotificationService _notificationService;

    public event EventHandler<TradingSignal>? SignalReceived;

    public async Task ConnectAsync(string serverUrl, string deviceId)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(serverUrl)
            .WithAutomaticReconnect()
            .Build();

        // 监听交易信号
        _connection.On<TradingSignal>("ReceiveTradingSignal", OnSignalReceived);

        // 监听通知
        _connection.On<object>("ReceiveNotification", OnNotificationReceived);

        await _connection.StartAsync();
        _logger.LogInformation("SignalR 连接成功");

        // 注册设备
        await _connection.InvokeAsync("RegisterDevice", deviceId, DeviceInfo.Name);
    }

    private void OnSignalReceived(TradingSignal signal)
    {
        _logger.LogInformation("收到交易信号: {Symbol} {Direction}",
            signal.Symbol, signal.Direction);

        // 触发震动
        Vibration.Vibrate(TimeSpan.FromSeconds(1));

        // 显示本地通知
        _notificationService.ShowNotification(
            "🔔 New Trading Signal",
            $"{signal.Symbol} {signal.Direction} @ {signal.EntryPrice}");

        // 触发事件
        SignalReceived?.Invoke(this, signal);
    }

    private void OnNotificationReceived(object notification)
    {
        // 处理通用通知
    }

    public async Task DisconnectAsync()
    {
        if (_connection != null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
        }
    }
}
```

**BackgroundListenerService.cs:**

```csharp
public class BackgroundListenerService : IHostedService
{
    private readonly SignalRService _signalRService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BackgroundListenerService> _logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("启动后台信号监听服务");

        var serverUrl = _configuration["SignalR:ServerUrl"];
        var deviceId = Preferences.Get("DeviceId", Guid.NewGuid().ToString());

        // 保存设备 ID
        Preferences.Set("DeviceId", deviceId);

        await _signalRService.ConnectAsync(serverUrl, deviceId);

        // 订阅信号
        _signalRService.SignalReceived += OnSignalReceived;
    }

    private void OnSignalReceived(object? sender, TradingSignal signal)
    {
        // 在主线程上导航到信号详情页
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Shell.Current.GoToAsync(
                $"SignalDetails?signalId={signal.SignalId}");
        });
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("停止后台信号监听服务");
        await _signalRService.DisconnectAsync();
    }
}
```

**OandaExecutionService.cs:**

```csharp
public class OandaExecutionService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OandaExecutionService> _logger;

    /// <summary>
    /// 执行市价单
    /// </summary>
    public async Task<OrderResult> ExecuteMarketOrderAsync(
        string symbol,
        string direction,
        double lotSize,
        double stopLoss,
        double takeProfit)
    {
        var apiKey = Preferences.Get("OandaApiKey", "");
        var accountId = Preferences.Get("OandaAccountId", "");

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(accountId))
        {
            throw new InvalidOperationException("OANDA credentials not configured");
        }

        _logger.LogInformation(
            "执行 {Direction} 订单: {Symbol}, Size: {LotSize}",
            direction, symbol, lotSize);

        // 构建 OANDA 请求
        var request = new
        {
            order = new
            {
                type = "MARKET",
                instrument = symbol,
                units = direction == "Buy" ? lotSize * 100000 : -lotSize * 100000,
                timeInForce = "FOK",
                stopLossOnFill = new { price = stopLoss.ToString("F5") },
                takeProfitOnFill = new { price = takeProfit.ToString("F5") }
            }
        };

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var response = await _httpClient.PostAsJsonAsync(
            $"https://api-fxpractice.oanda.com/v3/accounts/{accountId}/orders",
            request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("下单失败: {Error}", error);
            throw new Exception($"Order failed: {error}");
        }

        var result = await response.Content.ReadFromJsonAsync<OandaOrderResponse>();

        _logger.LogInformation("订单成功: Order ID = {OrderId}",
            result?.OrderFillTransaction?.Id);

        return new OrderResult
        {
            Success = true,
            OrderId = result?.OrderFillTransaction?.Id ?? "",
            ExecutedPrice = result?.OrderFillTransaction?.Price ?? 0
        };
    }
}
```

**MainPage.xaml:**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="TradingMobile.Views.MainPage"
             Title="Trading Agent">

    <ScrollView>
        <VerticalStackLayout Padding="20" Spacing="15">

            <!-- 连接状态 -->
            <Frame BorderColor="LightGray">
                <StackLayout>
                    <Label Text="Connection Status" FontSize="18" FontAttributes="Bold"/>
                    <Label Text="{Binding ConnectionStatus}" FontSize="14"/>
                    <Button Text="Reconnect" Command="{Binding ReconnectCommand}"
                            IsVisible="{Binding IsDisconnected}"/>
                </StackLayout>
            </Frame>

            <!-- 最新信号 -->
            <Frame BorderColor="Orange" BackgroundColor="LightYellow">
                <StackLayout>
                    <Label Text="Latest Signal" FontSize="18" FontAttributes="Bold"/>
                    <Label Text="{Binding LatestSignal.Symbol}" FontSize="16"/>
                    <Label Text="{Binding LatestSignal.Direction}" FontSize="16"
                           TextColor="Green"/>
                    <Label Text="{Binding LatestSignal.EntryPrice, StringFormat='Entry: {0:F2}'}"/>
                    <Label Text="{Binding LatestSignal.StopLoss, StringFormat='SL: {0:F2}'}"/>
                    <Label Text="{Binding LatestSignal.TakeProfit, StringFormat='TP: {0:F2}'}"/>

                    <Button Text="View Details"
                            Command="{Binding ViewSignalDetailsCommand}"
                            BackgroundColor="DodgerBlue" TextColor="White"/>
                </StackLayout>
            </Frame>

            <!-- 统计 -->
            <Frame BorderColor="LightGray">
                <Grid ColumnDefinitions="*,*" RowDefinitions="Auto,Auto">
                    <Label Grid.Row="0" Grid.Column="0"
                           Text="Today's Trades" FontSize="14"/>
                    <Label Grid.Row="0" Grid.Column="1"
                           Text="{Binding TodayTradesCount}" FontSize="14" HorizontalOptions="End"/>

                    <Label Grid.Row="1" Grid.Column="0"
                           Text="Today's P/L" FontSize="14"/>
                    <Label Grid.Row="1" Grid.Column="1"
                           Text="{Binding TodayProfitLoss, StringFormat='{0:C}'}"
                           FontSize="14" HorizontalOptions="End"/>
                </Grid>
            </Frame>

            <!-- 操作按钮 -->
            <Button Text="Settings" Command="{Binding OpenSettingsCommand}"/>
            <Button Text="Trade History" Command="{Binding OpenHistoryCommand}"/>

        </VerticalStackLayout>
    </ScrollView>

</ContentPage>
```

**SignalDetailsPage.xaml:**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="TradingMobile.Views.SignalDetailsPage"
             Title="Signal Details">

    <ScrollView>
        <VerticalStackLayout Padding="20" Spacing="15">

            <!-- 交易参数 -->
            <Frame BorderColor="DodgerBlue" BackgroundColor="AliceBlue">
                <StackLayout Spacing="10">
                    <Label Text="Trade Parameters"
                           FontSize="20" FontAttributes="Bold"/>

                    <Grid ColumnDefinitions="Auto,*" RowDefinitions="Auto,Auto,Auto,Auto,Auto"
                          RowSpacing="5">
                        <Label Grid.Row="0" Grid.Column="0" Text="Symbol:" FontAttributes="Bold"/>
                        <Label Grid.Row="0" Grid.Column="1" Text="{Binding Signal.Symbol}"/>

                        <Label Grid.Row="1" Grid.Column="0" Text="Direction:" FontAttributes="Bold"/>
                        <Label Grid.Row="1" Grid.Column="1" Text="{Binding Signal.Direction}"
                               TextColor="Green"/>

                        <Label Grid.Row="2" Grid.Column="0" Text="Entry:" FontAttributes="Bold"/>
                        <Label Grid.Row="2" Grid.Column="1"
                               Text="{Binding Signal.EntryPrice, StringFormat='{0:F2}'}"/>

                        <Label Grid.Row="3" Grid.Column="0" Text="Stop Loss:" FontAttributes="Bold"/>
                        <Label Grid.Row="3" Grid.Column="1"
                               Text="{Binding Signal.StopLoss, StringFormat='{0:F2}'}"/>

                        <Label Grid.Row="4" Grid.Column="0" Text="Take Profit:" FontAttributes="Bold"/>
                        <Label Grid.Row="4" Grid.Column="1"
                               Text="{Binding Signal.TakeProfit, StringFormat='{0:F2}'}"/>
                    </Grid>

                    <Label Text="{Binding Signal.SuggestedLotSize, StringFormat='Suggested Lot Size: {0:F2}'}"/>
                </StackLayout>
            </Frame>

            <!-- AI 分析 -->
            <Frame BorderColor="Purple" BackgroundColor="Lavender">
                <StackLayout Spacing="10">
                    <Label Text="AI Analysis" FontSize="20" FontAttributes="Bold"/>

                    <Label Text="L4 Reasoning:" FontAttributes="Bold"/>
                    <Label Text="{Binding Signal.L4_Reasoning}" FontSize="12"/>

                    <Label Text="Confidence Score:" FontAttributes="Bold"/>
                    <Label Text="{Binding Signal.ConfidenceScore, StringFormat='{0}/100'}"
                           FontSize="14" TextColor="Green"/>

                    <Button Text="View Full AI Thinking Process"
                            Command="{Binding ViewThinkingProcessCommand}"
                            BackgroundColor="Purple" TextColor="White"/>
                </StackLayout>
            </Frame>

            <!-- 风险警告 -->
            <Frame BorderColor="Red" BackgroundColor="MistyRose"
                   IsVisible="{Binding HasRiskFactors}">
                <StackLayout Spacing="5">
                    <Label Text="⚠️ Risk Factors"
                           FontSize="16" FontAttributes="Bold" TextColor="Red"/>
                    <Label Text="{Binding RiskFactorsText}" FontSize="12"/>
                </StackLayout>
            </Frame>

            <!-- 操作按钮 -->
            <Button Text="✅ Confirm &amp; Execute Trade"
                    Command="{Binding ExecuteTradeCommand}"
                    BackgroundColor="Green" TextColor="White"
                    FontSize="18" HeightRequest="60"/>

            <Button Text="❌ Reject"
                    Command="{Binding RejectTradeCommand}"
                    BackgroundColor="Red" TextColor="White"/>

            <!-- 倒计时 -->
            <Label Text="{Binding TimeRemaining, StringFormat='Signal expires in: {0}'}"
                   FontSize="12" HorizontalOptions="Center"/>

        </VerticalStackLayout>
    </ScrollView>

</ContentPage>
```

### 配置管理

**appsettings.json (Mobile):**

```json
{
  "SignalR": {
    "ServerUrl": "https://your-azure-functions.azurewebsites.net/api"
  },
  "Oanda": {
    "PracticeApiUrl": "https://api-fxpractice.oanda.com",
    "LiveApiUrl": "https://api-fxtrade.oanda.com"
  },
  "App": {
    "EnableNotifications": true,
    "VibrationEnabled": true,
    "AutoReconnect": true
  }
}
```

### 验收标准

