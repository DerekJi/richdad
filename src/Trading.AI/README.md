# Trading.AI - AI增强交易服务

## 概述

Trading.AI 是一个独立的AI服务层，集成Azure OpenAI，为交易系统提供智能分析能力。它不绑定特定策略，可用于多种交易场景。

## 核心功能

### 1. 多时间框架趋势分析
- 分析H1、H4、D1等多个时间框架
- 评估趋势方向（上升/下降/震荡）
- 计算趋势强度（0-100分）
- 识别价格结构和关键均线

### 2. 关键价格位识别
- 智能识别支撑位和阻力位
- 评估价格位强度
- 统计历史触碰次数
- 识别整数关口和密集成交区

### 3. 信号质量验证
- 验证Pin Bar等交易信号
- 综合评估多个维度
- 输出质量分数（0-100）
- 提供风险等级和操作建议

### 4. 智能缓存
- 趋势分析缓存6小时
- 关键价格位缓存12小时
- 减少90%重复API调用
- 显著降低成本

### 5. 成本控制
- 每日调用次数限制
- 月度预算监控
- 实时Token使用统计
- 自动降级保护

## 项目结构

```
Trading.AI/
├── Configuration/
│   ├── AzureOpenAISettings.cs      # Azure OpenAI配置
│   └── MarketAnalysisSettings.cs   # 市场分析配置
├── Models/
│   ├── TrendAnalysis.cs            # 趋势分析模型
│   ├── KeyLevelsAnalysis.cs        # 价格位分析模型
│   └── SignalValidation.cs         # 信号验证模型
├── Services/
│   ├── IAzureOpenAIService.cs      # OpenAI服务接口
│   ├── AzureOpenAIService.cs       # OpenAI服务实现
│   ├── IMarketAnalysisService.cs   # 市场分析接口
│   └── MarketAnalysisService.cs    # 市场分析实现
├── ServiceCollectionExtensions.cs  # 服务注册扩展
└── Trading.AI.csproj
```

## 快速开始

### 1. 安装依赖

项目已包含所需依赖：
- Azure.AI.OpenAI (2.1.0)
- Microsoft.Extensions.Caching.Memory
- Microsoft.Extensions.Configuration.Abstractions
- Microsoft.Extensions.DependencyInjection.Abstractions

### 2. 配置服务

在 `Program.cs` 中注册服务：

```csharp
using Trading.AI;

var builder = WebApplication.CreateBuilder(args);

// 添加Trading.AI服务
builder.Services.AddTradingAI();
```

### 3. 配置Azure OpenAI

在 `appsettings.json` 中添加配置：

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-api-key",
    "DeploymentName": "gpt-4o",
    "Enabled": true
  }
}
```

### 4. 使用服务

```csharp
public class MyService
{
    private readonly IMarketAnalysisService _analysisService;

    public MyService(IMarketAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    public async Task AnalyzeSignal()
    {
        // 验证Pin Bar信号
        var validation = await _analysisService.ValidatePinBarSignalAsync(
            symbol: "XAUUSD",
            pinBar: myPinBarCandle,
            direction: TradeDirection.Long
        );

        if (validation.IsValid)
        {
            Console.WriteLine($"信号质量: {validation.QualityScore}/100");
            Console.WriteLine($"建议: {validation.Recommendation}");
        }
    }
}
```

## API文档

### IMarketAnalysisService

#### AnalyzeMultiTimeFrameTrendAsync
分析多时间框架趋势

```csharp
Task<Dictionary<string, TrendAnalysis>> AnalyzeMultiTimeFrameTrendAsync(
    string symbol,
    List<string> timeFrames,
    Dictionary<string, List<Candle>> candlesByTimeFrame,
    CancellationToken cancellationToken = default
)
```

**参数**:
- `symbol`: 交易品种（如 XAUUSD）
- `timeFrames`: 时间框架列表（如 ["H1", "H4", "D1"]）
- `candlesByTimeFrame`: 每个时间框架的K线数据
- `cancellationToken`: 取消令牌

**返回**: 每个时间框架的趋势分析结果

**示例**:
```csharp
var trends = await _analysisService.AnalyzeMultiTimeFrameTrendAsync(
    "XAUUSD",
    new List<string> { "H1", "H4", "D1" },
    candlesDict
);

foreach (var (tf, analysis) in trends)
{
    Console.WriteLine($"{tf}: {analysis.Direction} (强度: {analysis.Strength})");
}
```

#### IdentifyKeyLevelsAsync
识别关键支撑和阻力位

```csharp
Task<KeyLevelsAnalysis> IdentifyKeyLevelsAsync(
    string symbol,
    List<Candle> candles,
    CancellationToken cancellationToken = default
)
```

**参数**:
- `symbol`: 交易品种
- `candles`: K线数据（建议200根）
- `cancellationToken`: 取消令牌

**返回**: 关键价格位分析结果

**示例**:
```csharp
var keyLevels = await _analysisService.IdentifyKeyLevelsAsync(
    "XAUUSD",
    recentCandles
);

Console.WriteLine("支撑位:");
foreach (var level in keyLevels.SupportLevels)
{
    Console.WriteLine($"  {level.Price:F5} (强度: {level.Strength})");
}
```

#### ValidatePinBarSignalAsync
验证Pin Bar信号质量

```csharp
Task<SignalValidation> ValidatePinBarSignalAsync(
    string symbol,
    Candle pinBar,
    TradeDirection direction,
    Dictionary<string, TrendAnalysis>? trendAnalyses = null,
    KeyLevelsAnalysis? keyLevels = null,
    CancellationToken cancellationToken = default
)
```

**参数**:
- `symbol`: 交易品种
- `pinBar`: Pin Bar K线
- `direction`: 交易方向（Long/Short）
- `trendAnalyses`: 趋势分析（可选，用于缓存）
- `keyLevels`: 关键价格位（可选，用于缓存）
- `cancellationToken`: 取消令牌

**返回**: 信号验证结果

**示例**:
```csharp
var validation = await _analysisService.ValidatePinBarSignalAsync(
    "XAUUSD",
    pinBarCandle,
    TradeDirection.Long
);

if (validation.QualityScore >= 70)
{
    Console.WriteLine($"高质量信号: {validation.QualityScore}/100");
    Console.WriteLine($"风险: {validation.Risk}");
    Console.WriteLine($"建议: {validation.Recommendation}");
}
```

### IAzureOpenAIService

#### ChatCompletionAsync
发送聊天请求

```csharp
Task<string> ChatCompletionAsync(
    string systemPrompt,
    string userMessage,
    CancellationToken cancellationToken = default
)
```

#### GetTodayUsageCount
获取今日调用次数

```csharp
int GetTodayUsageCount()
```

#### GetEstimatedMonthlyCost
获取本月预估成本

```csharp
decimal GetEstimatedMonthlyCost()
```

## 最佳实践

### 1. 使用缓存

趋势分析和关键价格位会自动缓存，建议复用：

```csharp
// 早晨分析一次趋势（缓存6小时）
var trends = await _analysisService.AnalyzeMultiTimeFrameTrendAsync(...);

// 后续信号验证会使用缓存的趋势数据
var validation = await _analysisService.ValidatePinBarSignalAsync(
    symbol, pinBar, direction,
    trendAnalyses: trends  // 复用缓存
);
```

### 2. 代码预筛

先用代码快速筛选，只对潜在信号使用AI：

```csharp
// 代码快速筛选
if (strategy.CanOpenLong(current, previous, false))
{
    // 只对通过初步筛选的信号使用AI验证
    var validation = await _analysisService.ValidatePinBarSignalAsync(...);

    if (validation.IsValid)
    {
        // 发送信号
    }
}
```

### 3. 错误处理

AI失败不应影响系统运行：

```csharp
SignalValidation? aiValidation = null;
try
{
    aiValidation = await _analysisService.ValidatePinBarSignalAsync(...);
}
catch (Exception ex)
{
    _logger.LogError(ex, "AI验证失败，继续发送信号");
    // 继续执行，不依赖AI结果
}
```

### 4. 监控成本

定期检查使用量和成本：

```csharp
var todayUsage = _openAIService.GetTodayUsageCount();
var monthlyCost = _openAIService.GetEstimatedMonthlyCost();

_logger.LogInformation(
    "AI使用统计 - 今日调用: {Count}, 本月成本: ${Cost:F2}",
    todayUsage, monthlyCost
);
```

## 配置优化

### 开发环境（频繁测试）

```json
{
  "AzureOpenAI": {
    "DeploymentName": "gpt-3.5-turbo",  // 使用经济模型
    "MaxDailyRequests": 1000,
    "Temperature": 0.5
  },
  "MarketAnalysis": {
    "TrendAnalysisCacheMinutes": 60,    // 短缓存便于测试
    "KeyLevelsCacheMinutes": 120
  }
}
```

### 生产环境（追求质量）

```json
{
  "AzureOpenAI": {
    "DeploymentName": "gpt-4o",         // 使用高质量模型
    "MaxDailyRequests": 500,
    "Temperature": 0.3,                  // 更确定性
    "MonthlyBudgetLimit": 50.0
  },
  "MarketAnalysis": {
    "TrendAnalysisCacheMinutes": 360,   // 长缓存节省成本
    "KeyLevelsCacheMinutes": 720,
    "MinSignalQualityScore": 70          // 更高质量要求
  }
}
```

## 扩展性

### 添加新的分析服务

1. 在 `Services/` 创建新接口
2. 实现服务类
3. 在 `ServiceCollectionExtensions.cs` 注册
4. 注入使用

### 支持其他AI模型

修改 `AzureOpenAIService.cs` 支持其他模型：

```csharp
// 支持不同模型
if (_settings.DeploymentName.Contains("gpt-4"))
{
    options.MaxOutputTokenCount = 4000;
}
else if (_settings.DeploymentName.Contains("gpt-3.5"))
{
    options.MaxOutputTokenCount = 2000;
}
```

## 故障排查

查看 [Azure OpenAI 配置指南](../../docs/AZURE_OPENAI_SETUP.md#故障排查) 了解常见问题解决方案。

## 许可

本项目是Trading System的一部分，遵循相同的许可协议。
