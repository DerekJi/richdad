# Azure OpenAI 集成配置指南

## 概述

Trading.Infrastructure.AI 是集成在Trading.Infrastructure项目中的AI服务模块，集成了Azure OpenAI，用于增强交易信号质量。它不绑定特定策略，可以用于多种场景。

## 功能特性

- ✅ **多时间框架趋势分析**（H1/H4/D1）
- ✅ **智能识别支撑阻力位**
- ✅ **Pin Bar信号质量验证**
- ✅ **智能缓存**（6-12小时）减少API调用
- ✅ **成本控制**（每日限额、月度预算）
- ✅ **自动降级**（AI失败不影响信号发送）

## 配置步骤

### 1. 在Azure门户创建OpenAI资源

1. 登录 [Azure Portal](https://portal.azure.com)
2. 搜索 "Azure OpenAI"
3. 点击 **Create** 创建资源
4. 选择订阅和资源组
5. 选择区域（推荐 East US 或 West Europe）
6. 创建完成后，进入资源

### 2. 部署模型

1. 在Azure OpenAI资源中，点击 **Model deployments**
2. 点击 **Create new deployment**
3. 选择模型：**gpt-4o**（推荐）或 **gpt-3.5-turbo**（经济）
4. 设置部署名称（如 `gpt-4o`）
5. 点击 **Create**

### 3. 获取配置信息

在Azure OpenAI资源页面：

- **Endpoint**: 在 "Keys and Endpoint" 页面获取
  - 格式：`https://YOUR-RESOURCE-NAME.openai.azure.com/`
- **API Key**: 在 "Keys and Endpoint" 页面获取
  - 使用 Key 1 或 Key 2

### 4. 配置应用程序

#### 方式1：appsettings.json（开发环境）

在 `Trading.Web/appsettings.json` 中添加：

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://YOUR-RESOURCE-NAME.openai.azure.com/",
    "ApiKey": "YOUR-API-KEY-HERE",
    "DeploymentName": "gpt-4o",
    "Enabled": true,
    "MaxDailyRequests": 500,
    "MonthlyBudgetLimit": 50.0,
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "Temperature": 0.3,
    "MaxTokens": 2000
  },
  "MarketAnalysis": {
    "TrendAnalysisCacheMinutes": 360,
    "KeyLevelsCacheMinutes": 720,
    "TrendAnalysisCandles": 100,
    "KeyLevelsCandles": 200,
    "EnableDetailedLogging": false,
    "MinSignalQualityScore": 60
  }
}
```

#### 方式2：User Secrets（推荐生产环境）

```bash
cd src/Trading.Web

dotnet user-secrets set "AzureOpenAI:Endpoint" "https://YOUR-RESOURCE-NAME.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:ApiKey" "YOUR-API-KEY-HERE"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4o"
dotnet user-secrets set "AzureOpenAI:Enabled" "true"
```

#### 方式3：环境变量

```bash
# Windows PowerShell
$env:AzureOpenAI__Endpoint="https://YOUR-RESOURCE-NAME.openai.azure.com/"
$env:AzureOpenAI__ApiKey="YOUR-API-KEY-HERE"
$env:AzureOpenAI__DeploymentName="gpt-4o"
$env:AzureOpenAI__Enabled="true"

# Linux/Mac
export AzureOpenAI__Endpoint="https://YOUR-RESOURCE-NAME.openai.azure.com/"
export AzureOpenAI__ApiKey="YOUR-API-KEY-HERE"
export AzureOpenAI__DeploymentName="gpt-4o"
export AzureOpenAI__Enabled="true"
```

### 5. 在应用程序中启用AI服务

在 `Program.cs` 中添加服务注册：

```csharp
using Trading.Infrastructure.AI;

var builder = WebApplication.CreateBuilder(args);

// 添加Trading.Infrastructure.AI服务
builder.Services.AddTradingAI();

// ... 其他服务

var app = builder.Build();
```

## 配置参数说明

### AzureOpenAI 配置

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Endpoint` | string | - | Azure OpenAI端点URL |
| `ApiKey` | string | - | API密钥 |
| `DeploymentName` | string | gpt-4o | 模型部署名称 |
| `Enabled` | bool | false | 是否启用AI功能 |
| `MaxDailyRequests` | int | 500 | 每日最大调用次数 |
| `MonthlyBudgetLimit` | decimal | 50.0 | 月度预算限制（美元） |
| `TimeoutSeconds` | int | 30 | 请求超时时间 |
| `MaxRetries` | int | 3 | 最大重试次数 |
| `Temperature` | float | 0.3 | 温度参数（0-2） |
| `MaxTokens` | int | 2000 | 最大输出Token数 |

### MarketAnalysis 配置

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `TrendAnalysisCacheMinutes` | int | 360 | 趋势分析缓存时长（分钟） |
| `KeyLevelsCacheMinutes` | int | 720 | 关键价格位缓存时长（分钟） |
| `TrendAnalysisCandles` | int | 100 | 趋势分析使用的K线数量 |
| `KeyLevelsCandles` | int | 200 | 价格位识别使用的K线数量 |
| `EnableDetailedLogging` | bool | false | 是否启用详细日志 |
| `MinSignalQualityScore` | int | 60 | 最小信号质量分数 |

## 成本控制

### 预估成本

**GPT-4o** (推荐):
- 价格：~$5 per 1M tokens
- 每次分析：~1500 tokens
- 月成本（每天10次）：~$2.25

**GPT-3.5-turbo** (经济):
- 价格：~$0.5 per 1M tokens
- 月成本（每天10次）：~$0.23

### 优化策略

1. **智能缓存**
   - 趋势分析缓存6小时
   - 关键价格位缓存12小时
   - 减少90%重复调用

2. **每日限额**
   - 设置 `MaxDailyRequests` 控制调用次数
   - 达到限额后自动停止AI调用

3. **月度预算**
   - 设置 `MonthlyBudgetLimit` 监控成本
   - 实时跟踪Token使用量

4. **代码预筛**
   - AI只验证通过初步筛选的信号
   - 降低无效调用

## 使用示例

### 1. 验证Pin Bar信号

```csharp
var validation = await _marketAnalysisService.ValidatePinBarSignalAsync(
    symbol: "XAUUSD",
    pinBar: pinBarCandle,
    direction: TradeDirection.Long
);

if (validation.IsValid && validation.QualityScore >= 70)
{
    // 发送高质量信号
    Console.WriteLine($"质量分数: {validation.QualityScore}/100");
    Console.WriteLine($"建议: {validation.Recommendation}");
}
```

### 2. 分析多时间框架趋势

```csharp
var trends = await _marketAnalysisService.AnalyzeMultiTimeFrameTrendAsync(
    symbol: "XAUUSD",
    timeFrames: new List<string> { "H1", "H4", "D1" },
    candlesByTimeFrame: candlesDict
);

foreach (var (tf, analysis) in trends)
{
    Console.WriteLine($"{tf}: {analysis.Direction} (强度: {analysis.Strength})");
}
```

### 3. 识别关键价格位

```csharp
var keyLevels = await _marketAnalysisService.IdentifyKeyLevelsAsync(
    symbol: "XAUUSD",
    candles: recentCandles
);

Console.WriteLine($"支撑位: {string.Join(", ", keyLevels.SupportLevels.Select(s => s.Price))}");
Console.WriteLine($"阻力位: {string.Join(", ", keyLevels.ResistanceLevels.Select(r => r.Price))}");
```

## 监控和日志

### 查看AI使用统计

```csharp
var todayUsage = _openAIService.GetTodayUsageCount();
var monthlyCost = _openAIService.GetEstimatedMonthlyCost();

Console.WriteLine($"今日调用次数: {todayUsage}");
Console.WriteLine($"本月预估成本: ${monthlyCost:F2}");
```

### 日志级别

在 `appsettings.json` 中配置：

```json
{
  "Logging": {
    "LogLevel": {
      "Trading.AI": "Information",
      "Trading.Infrastructure.AI.Services.MarketAnalysisService": "Debug"
    }
  }
}
```

## 故障排查

### 1. AI服务未启用

**错误**: `Azure OpenAI服务未启用`

**解决**: 检查 `AzureOpenAI:Enabled` 是否设置为 `true`

### 2. 端点或密钥错误

**错误**: `Azure OpenAI Endpoint 和 ApiKey 必须配置`

**解决**: 确认配置文件中正确设置了 Endpoint 和 ApiKey

### 3. 达到每日限额

**错误**: `已达到每日调用限制: 500`

**解决**:
- 等待第二天自动重置
- 或增加 `MaxDailyRequests` 配置值

### 4. 超时错误

**错误**: Request timeout

**解决**: 增加 `TimeoutSeconds` 配置值

## 禁用AI功能

如果需要临时禁用AI功能：

```json
{
  "AzureOpenAI": {
    "Enabled": false
  }
}
```

服务会自动降级，不影响原有信号发送功能。

## 安全建议

1. ✅ **生产环境使用User Secrets或环境变量**，不要在代码中硬编码API密钥
2. ✅ **定期轮换API密钥**
3. ✅ **设置合理的成本限制**
4. ✅ **监控每日使用量和成本**
5. ✅ **使用Azure RBAC控制访问权限**

## 下一步

- 查看 [Trading.AI项目README](../src/Trading.AI/README.md) 了解更多技术细节
- 参考 [信号验证策略](./SIGNAL_VALIDATION_STRATEGY.md) 了解AI验证逻辑
