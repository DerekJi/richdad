# 双级AI架构 - 快速开始

## 概述

本文档帮助你快速启用双级AI过滤架构，实现每15分钟一次的高频交易信号分析，同时将Azure OpenAI成本降低75-80%。

## 前置条件

- ✅ Azure OpenAI 账户已配置
- ✅ 已部署 `gpt-4o-mini` 模型
- ✅ 已部署 `gpt-4o` 模型
- ✅ Telegram Bot 已配置（用于发送交易信号）

## 快速启动步骤

### 1️⃣ 配置 Azure OpenAI

编辑 `src/Trading.AlertSystem.Web/appsettings.json`:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://YOUR-RESOURCE.openai.azure.com/",
    "ApiKey": "YOUR-API-KEY",
    "Enabled": true
  },
  "DualTierAI": {
    "Enabled": true,
    "Tier1MinScore": 70,
    "Tier1": {
      "DeploymentName": "gpt-4o-mini",  // 你的部署名称
      "Temperature": 0.3,
      "MaxTokens": 500
    },
    "Tier2": {
      "DeploymentName": "gpt-4o",       // 你的部署名称
      "Temperature": 0.5,
      "MaxTokens": 2000
    },
    "MaxDailyRequests": 500,
    "MonthlyBudgetLimit": 50.0
  }
}
```

### 2️⃣ 注册服务

在 `src/Trading.AlertSystem.Web/Program.cs` 中添加：

```csharp
using Trading.AI.Configuration;
using Trading.AI.Services;
using Trading.AlertSystem.Service.Services;

// ... 现有代码 ...

// 注册Azure OpenAI配置
builder.Services.Configure<AzureOpenAISettings>(
    builder.Configuration.GetSection(AzureOpenAISettings.SectionName));

// 注册双级AI配置
builder.Services.Configure<DualTierAISettings>(
    builder.Configuration.GetSection(DualTierAISettings.SectionName));

// 注册双级AI服务
builder.Services.AddSingleton<IDualTierAIService, DualTierAIService>();

// 注册双级监控服务（替换现有的 PinBarMonitoringService）
builder.Services.AddHostedService<DualTierPinBarMonitoringService>();

// ... 现有代码 ...
```

### 3️⃣ 更新 PinBar 监控配置

确保 PinBar 监控配置已启用：

```json
{
  "PinBarMonitoring": {
    "Enabled": true,
    "Symbols": ["XAUUSD", "XAGUSD"],
    "TimeFrames": ["M15", "H1"],
    "CheckIntervalMinutes": 15,
    "StrategySettings": {
      "MinBodyToWickRatio": 0.33,
      "MinWickToBodyRatio": 2.0,
      "MaxBodySize": 0.3,
      "RiskRewardRatio": 2.0
    }
  }
}
```

### 4️⃣ 启动服务

```bash
cd src/Trading.AlertSystem.Web
dotnet run
```

## 验证运行

### 查看日志

启动后，你应该看到类似的日志：

```
✅ 双级AI架构已启用 - 成本优化模式
🚀 双级AI PinBar监控服务已启动 - 检查间隔: 15分钟
```

### 观察工作流程

#### Tier1 拦截示例（无需调用 Tier2）

```
🚫 Tier1拦截 - XAUUSD M15 | 评分: 45/100 | 趋势: Neutral | 
原因: 市场横盘震荡，无明显趋势 | 成本: $0.0003 | 耗时: 1850ms
📊 今日统计 - Tier1调用: 25, Tier2调用: 5, 拦截: 20, 成本: $0.15
```

#### Tier2 通过示例（发送交易信号）

```
✅ Tier2完成 - XAUUSD M15 | Tier1评分: 85 | 动作: BUY | 
入场: 2755.50 | 止损: 2750.00 | 止盈: 2770.00 | 
风险: $35.00 | RR比: 2.64 | 总成本: $0.0250 | 总耗时: 8500ms
```

## 监控和优化

### 查看统计信息

每次分析完成后，日志会输出今日统计：

```
📊 今日统计 - Tier1调用: 96, Tier2调用: 20, 拦截: 76, 成本: $2.50
```

### 成本估算

- **Tier1 成本**: ~$0.0003 每次
- **Tier2 成本**: ~$0.022 每次
- **拦截率**: 60-80%

**示例**（每天96次分析，拦截率75%）：
- Tier1: 96 × $0.0003 = $0.029
- Tier2: 24 × $0.022 = $0.528
- **总计**: $0.56/天 = **$16.80/月**

### 调整阈值

如果拦截率太高或太低，可以调整 `Tier1MinScore`：

- **拦截率太高**（错过太多信号）: 降低到 60-65
- **拦截率太低**（成本过高）: 提高到 75-80

```json
{
  "DualTierAI": {
    "Tier1MinScore": 70  // 调整这个值
  }
}
```

## Telegram 消息格式

成功通过双级验证的信号会发送如下消息：

```
🟢 **PinBar 做多信号 [双级AI验证通过]**

**品种**: XAUUSD
**周期**: M15
**信号时间**: 2026-02-06 10:15:00 UTC

📊 **AI推荐交易参数**:
• 入场价: 2755.50
• 止损价: 2750.00
• 止盈价: 2770.00
• 风险金额: $35.00
• 盈亏比: 2.64

🤖 **Tier1快速评估** (GPT-4o-mini):
• 机会评分: 85/100
• 趋势方向: Bullish
• 初步判断: 突破关键阻力位，上涨动能强劲

🎯 **Tier2深度分析** (GPT-4o):
• 动作建议: BUY
• 支撑位分析: 2750支撑强劲，多次测试未破
• 阻力位分析: 突破2755，目标2770
• 假突破风险: 低，成交量确认
• 多周期共振: M15、H1均向上

💡 **AI推理**:
价格已突破关键阻力位2755，多个周期共振向上...
```

## 故障排查

### 问题：Tier1 总是评分很低

**解决方案**：
1. 检查市场数据质量
2. 确认时间周期配置正确
3. 查看日志中的具体拦截原因
4. 考虑降低 `Tier1MinScore` 到 60

### 问题：成本过高

**解决方案**：
1. 提高 `Tier1MinScore` 到 75-80
2. 减少监控的品种或时间周期
3. 增加检查间隔到 20-30 分钟
4. 检查是否有重复调用

### 问题：服务启动失败

**解决方案**：
1. 检查 Azure OpenAI 配置是否正确
2. 验证模型部署名称是否匹配
3. 确认 API Key 是否有效
4. 查看完整的错误日志

## 下一步

1. **监控效果**：观察 1-2 天，收集数据
2. **优化阈值**：根据实际效果调整 `Tier1MinScore`
3. **扩展品种**：效果良好后，增加更多交易品种
4. **调整频率**：根据需要调整检查间隔

## 性能指标

- **响应时延**: Tier1 < 2s, 总计 < 10s
- **成本优化**: 相比纯 GPT-4o 节省 75-80%
- **拦截率**: 预期 60-80%
- **准确率**: 需要实际运行数据验证

## 相关文档

- [详细使用指南](./DUAL_TIER_AI_GUIDE.md)
- [实现总结](./DUAL_TIER_AI_IMPLEMENTATION.md)
- [Azure OpenAI 配置](./AZURE_OPENAI_SETUP.md)

## 支持

如有问题，请查看日志输出或参考详细文档。

---

**版本**: 1.0  
**更新时间**: 2026-02-06
