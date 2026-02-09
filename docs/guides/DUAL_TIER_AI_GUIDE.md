# 双级AI过滤架构 - 使用指南

## 概述

双级AI过滤架构通过两个层次的模型来优化交易信号验证的成本与精度：

1. **Tier 1 (GPT-4o-mini)**: 快速过滤层，用于识别潜在交易机会
2. **Tier 2 (GPT-4o)**: 深度分析层，用于确认高质量交易信号

## 架构优势

- **成本优化**: Tier1可以过滤掉60%-80%的低质量信号，大幅降低Azure OpenAI成本
- **精度保证**: 只有通过Tier1的信号才会进入Tier2进行深度分析
- **快速响应**: Tier1响应时间<2秒，总链路时延<10秒
- **智能拦截**: 自动记录拦截原因，便于分析和优化

## 配置说明

### 1. appsettings.json 配置

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-endpoint.openai.azure.com/",
    "ApiKey": "your-api-key",
    "DeploymentName": "gpt-4o",
    "Enabled": true
  },
  "DualTierAI": {
    "Enabled": true,
    "Tier1MinScore": 70,
    "Tier1": {
      "DeploymentName": "gpt-4o-mini",
      "Temperature": 0.3,
      "MaxTokens": 500,
      "CostPer1MInputTokens": 0.15,
      "CostPer1MOutputTokens": 0.60
    },
    "Tier2": {
      "DeploymentName": "gpt-4o",
      "Temperature": 0.5,
      "MaxTokens": 2000,
      "CostPer1MInputTokens": 2.50,
      "CostPer1MOutputTokens": 10.00
    },
    "IncludeTier1SummaryInTier2": true,
    "MaxDailyRequests": 500,
    "MonthlyBudgetLimit": 50.0
  }
}
```

### 2. 依赖注入配置

在 `Program.cs` 或启动类中：

```csharp
// 注册Azure OpenAI配置
builder.Services.Configure<AzureOpenAISettings>(
    builder.Configuration.GetSection(AzureOpenAISettings.SectionName));

// 注册双级AI配置
builder.Services.Configure<DualTierAISettings>(
    builder.Configuration.GetSection(DualTierAISettings.SectionName));

// 注册双级AI服务
builder.Services.AddSingleton<IDualTierAIService, DualTierAIService>();

// 注册双级监控服务（如果需要）
builder.Services.AddSingleton<IDualTierMonitoringService, DualTierMonitoringService>();
```

## 使用示例

### 基本用法

```csharp
// 注入服务
private readonly IDualTierAIService _dualTierAI;

public MyService(IDualTierAIService dualTierAI)
{
    _dualTierAI = dualTierAI;
}

// 执行双级分析
public async Task AnalyzeMarketAsync()
{
    var marketData = PrepareMarketData(); // 准备市场数据
    var symbol = "XAUUSD";

    var result = await _dualTierAI.AnalyzeAsync(marketData, symbol);

    // 检查是否通过Tier1
    if (!result.PassedTier1)
    {
        _logger.LogInformation("Tier1过滤未通过 - Score: {Score}, Reason: {Reason}",
            result.Tier1Result.OpportunityScore,
            result.Tier1Result.RejectionReason);
        return;
    }

    // 检查是否建议入场
    if (result.ShouldEnter && result.Tier2Result != null)
    {
        _logger.LogInformation("建议入场 - Entry: {Entry}, SL: {SL}, TP: {TP}",
            result.Tier2Result.EntryPrice,
            result.Tier2Result.StopLoss,
            result.Tier2Result.TakeProfit);

        // 发送Telegram通知或执行交易
        await SendTradingSignalAsync(result);
    }
}
```

### 仅执行Tier1过滤

```csharp
// 快速评估市场机会
var tier1Result = await _dualTierAI.ExecuteTier1FilterAsync(marketData, symbol);

if (tier1Result.OpportunityScore >= 70)
{
    _logger.LogInformation("发现高质量机会，继续深度分析");
    var tier2Result = await _dualTierAI.ExecuteTier2AnalysisAsync(
        marketData, symbol, tier1Result);
}
```

### 监控统计信息

```csharp
// 获取今日使用情况
var todayUsage = _dualTierAI.GetTodayUsageCount();
var estimatedCost = _dualTierAI.GetEstimatedMonthlyCost();

_logger.LogInformation("今日调用次数: {Count}, 本月预估成本: ${Cost:F2}",
    todayUsage, estimatedCost);

// 检查是否达到限额
if (_dualTierAI.IsRateLimitReached())
{
    _logger.LogWarning("已达到每日调用限制");
}
```

## 数据格式要求

### 市场数据格式

市场数据应该是压缩的CSV格式或多周期数据：

```csv
DateTime,Open,High,Low,Close,Volume
2026-02-06 10:00,2750.50,2755.20,2748.30,2752.80,1200
2026-02-06 10:15,2752.80,2758.00,2751.50,2756.20,1350
...
```

或使用结构化数据：

```json
{
  "M5": [...],
  "M15": [...],
  "H1": [...]
}
```

## 日志输出示例

### Tier1拦截日志

```
🚫 Tier1拦截 - XAUUSD M15 | 评分: 45/100 | 趋势: Neutral |
原因: 市场横盘震荡，无明显趋势 | 成本: $0.0003 | 耗时: 1850ms
📊 今日统计 - Tier1调用: 25, Tier2调用: 5, 拦截: 20, 成本: $0.15
```

### Tier2通过日志

```
✅ Tier2完成 - XAUUSD M15 | Tier1评分: 85 | 动作: BUY |
入场: 2755.50 | 止损: 2750.00 | 止盈: 2770.00 |
风险: $35.00 | RR比: 2.64 | 总成本: $0.0250 | 总耗时: 8500ms
📊 今日统计 - Tier1调用: 26, Tier2调用: 6, 拦截: 20, 成本: $0.18
```

## 成本分析

### 成本对比

| 场景 | 传统方式 (仅GPT-4o) | 双级架构 | 节省 |
|------|---------------------|----------|------|
| 100次分析（80%拦截） | $2.50 | $0.62 | 75% |
| 月度成本（每15分钟一次） | $180 | $45 | 75% |

### 实际案例

假设每天分析96次（每15分钟一次）：
- **传统方式**: 96次 × $0.025 = $2.40/天 × 30天 = **$72/月**
- **双级架构**:
  - Tier1: 96次 × $0.0003 = $0.03/天
  - Tier2: 20次 × $0.022 = $0.44/天
  - 总计: $0.47/天 × 30天 = **$14/月**
  - **节省: $58/月 (81%)**

## 最佳实践

1. **阈值调整**: 根据实际效果调整 `Tier1MinScore`，建议范围 60-80
2. **批量处理**: 如果有多个信号需要分析，可以考虑批量处理
3. **缓存策略**: 对于相同的市场数据，可以使用缓存避免重复调用
4. **错误处理**: 实现降级策略，AI服务失败时不影响核心交易逻辑
5. **监控告警**: 设置成本告警，防止超出预算

## 故障排查

### 常见问题

1. **Tier1总是拦截**
   - 检查 `Tier1MinScore` 是否设置过高
   - 查看日志中的拦截原因
   - 验证市场数据质量

2. **成本过高**
   - 检查调用频率是否合理
   - 验证 `MaxDailyRequests` 配置
   - 查看是否有重复调用

3. **响应时间过长**
   - 检查网络连接
   - 优化市场数据大小
   - 考虑调整 `MaxTokens` 限制

## 下一步

- [ ] 在测试环境中验证双级AI效果
- [ ] 监控实际成本和拦截率
- [ ] 根据数据优化Tier1阈值
- [ ] 集成到生产监控服务
- [ ] 设置成本告警和日报

## 相关文档

- [Azure OpenAI配置](./AZURE_OPENAI_SETUP.md)
- [PinBar策略快速开始](./PINBAR_QUICKSTART.md)
- [成本优化指南](./COST_OPTIMIZATION.md)
