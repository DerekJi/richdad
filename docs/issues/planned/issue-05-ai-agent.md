## Issue 5: 实现 AI Agent 无代码交易系统

### 标题
🤖 Implement AI Trading Agent with Natural Language Interface

### 描述
实现基于 OpenAI Function Calling 的 AI Trading Agent，允许用户通过自然语言 Prompt 执行复杂的交易任务，无需手动编写代码或调用API。

### 背景
当前系统虽然功能完善，但每次执行任务都需要：
- 手动调用多个API
- 编写代码组合不同服务
- 理解复杂的参数配置

通过实现 AI Agent，用户可以：
- **自然语言交互**：用一句话描述任务，AI自动执行
- **智能任务编排**：AI自动决定调用顺序和参数
- **多步骤自动化**：复杂任务一次完成
- **降低使用门槛**：不需要编程知识

### 示例场景

**简单任务：**
```
用户: "获取最新的黄金5分钟K线图120根，导入到数据库"

AI Agent 自动:
1. 调用 get_oanda_candles("XAU_USD", "M5", 120)
2. 调用 save_to_database("Candles", data)
3. 返回: "已保存120根黄金M5 K线到数据库"
```

**复杂任务：**
```
用户: "获取黄金的M5最新120根、H1最新80根、D1最新100根K线，
      格式化为Markdown（包含EMA20和Dist_EMA20），
      然后用GPT-4o按Al Brooks理论分析是否应该开仓，
      如果要开仓就按FTMO风控计算仓位并执行，
      所有结果都要保存到数据库"

AI Agent 自动:
1. 获取3个时间框架的数据
2. 计算EMA20指标
3. 格式化为Markdown表格
4. 调用GPT-4o进行Al Brooks理论分析
5. 根据分析结果决定是否开仓
6. 如果开仓，计算FTMO风控仓位
7. 执行订单
8. 保存所有中间结果和最终决策
9. 返回完整执行报告
```

### 实现功能

#### ✅ 1. 创建 AI Agent 项目

**新项目：** `Trading.AI.Agent`

```
src/Trading.AI.Agent/
├── Trading.AI.Agent.csproj
├── Services/
│   ├── TradingAgentService.cs          # 核心Agent服务
│   ├── DataFormatterService.cs         # 数据格式化
│   └── AgentToolRegistry.cs            # 工具注册管理
├── Controllers/
│   └── AgentController.cs              # API接口
├── Models/
│   ├── AgentRequest.cs                 # 请求模型
│   ├── AgentResponse.cs                # 响应模型
│   └── ExecutionStep.cs                # 执行步骤
└── Configuration/
    └── AgentSettings.cs                # Agent配置
```

**依赖项：**
```xml
<ItemGroup>
  <PackageReference Include="Azure.AI.OpenAI" Version="2.1.0" />
  <PackageReference Include="Skender.Stock.Indicators" Version="2.7.1" />

  <ProjectReference Include="..\Trading.AI\Trading.AI.csproj" />
  <ProjectReference Include="..\Trading.Infras.Data\Trading.Infras.Data.csproj" />
  <ProjectReference Include="..\Trading.Infras.Service\Trading.Infras.Service.csproj" />
  <ProjectReference Include="..\Trading.Core\Trading.Core.csproj" />
</ItemGroup>
```

#### ✅ 2. 核心服务：TradingAgentService

**功能：**
- 定义可用的工具（Function Definitions）
- 处理用户 Prompt
- 调用 GPT-4o-mini 进行任务理解和编排
- 执行工具函数
- 返回执行结果

**主要方法：**

```csharp
public class TradingAgentService
{
    private readonly AzureOpenAIClient _aiClient;
    private readonly IOandaService _oandaService;
    private readonly IMarketAnalysisService _analysisService;
    private readonly RiskManager _riskManager;
    private readonly IOrderExecutionService _orderService;
    private readonly DataFormatterService _formatter;
    private readonly IAlertHistoryRepository _historyRepo;
    private readonly IAIAnalysisRepository _aiAnalysisRepo;
    private readonly ILogger<TradingAgentService> _logger;

    // 工具定义
    private readonly ChatTool[] _tools;

    /// <summary>
    /// 执行用户Prompt
    /// </summary>
    public async Task<AgentResponse> ExecutePrompt(
        string prompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行具体的工具函数
    /// </summary>
    private async Task<string> ExecuteFunction(
        string functionName,
        string argumentsJson);
}
```

#### ✅ 3. 工具定义（8个核心工具）

**工具1: get_oanda_candles**
```csharp
ChatTool.CreateFunctionTool(
    functionName: "get_oanda_candles",
    functionDescription: """
        从OANDA获取历史K线数据。
        支持的时间框架: M1, M5, M15, M30, H1, H4, D1, W1, MN1
        支持的品种: XAU_USD(黄金), XAG_USD(白银), EUR_USD, GBP_USD等
        返回JSON格式的K线数组，包含time, open, high, low, close, volume
        """,
    functionParameters: BinaryData.FromString("""
    {
        "type": "object",
        "properties": {
            "symbol": {
                "type": "string",
                "description": "交易品种，如XAU_USD(黄金), EUR_USD(欧美)",
                "enum": ["XAU_USD", "XAG_USD", "EUR_USD", "GBP_USD", "USD_JPY"]
            },
            "timeframe": {
                "type": "string",
                "description": "时间框架",
                "enum": ["M1", "M5", "M15", "M30", "H1", "H4", "D1", "W1", "MN1"]
            },
            "count": {
                "type": "integer",
                "description": "K线数量，建议50-500根",
                "minimum": 1,
                "maximum": 5000
            }
        },
        "required": ["symbol", "timeframe", "count"]
    }
    """)
)
```

**工具2: format_candles_to_markdown**
```csharp
ChatTool.CreateFunctionTool(
    functionName: "format_candles_to_markdown",
    functionDescription: """
        将K线数据格式化为Markdown表格，包含以下列：
        - Date: 日期（MMDD格式，可选包含年份）
        - Time: 时间（HHMM格式）
        - Open, High, Low, Close: OHLC价格
        - BodyRange: High - Low
        - Body%: (Close - Open) / BodyRange * 100
        - EMA20: 20周期指数移动平均线
        - Dist_EMA20: Low - EMA20
        """,
    functionParameters: BinaryData.FromString("""
    {
        "type": "object",
        "properties": {
            "candles_json": {
                "type": "string",
                "description": "K线数据的JSON字符串（get_oanda_candles返回的结果）"
            },
            "ema_period": {
                "type": "integer",
                "description": "EMA周期，默认20",
                "default": 20
            },
            "include_year": {
                "type": "boolean",
                "description": "日期是否包含年份，默认false",
                "default": false
            }
        },
        "required": ["candles_json"]
    }
    """)
)
```

**工具3: analyze_market_with_gpt4o**
```csharp
ChatTool.CreateFunctionTool(
    functionName: "analyze_market_with_gpt4o",
    functionDescription: """
        使用GPT-4o分析市场数据，基于Al Brooks价格行为理论给出交易建议。
        可以分析单个或多个时间框架的数据。
        返回分析结果包括：
        - 是否建议开仓
        - 开仓方向（buy/sell）
        - 建议入场价
        - 建议止损价
        - 建议止盈价
        - 信号质量评分（0-100）
        - 详细分析理由
        """,
    functionParameters: BinaryData.FromString("""
    {
        "type": "object",
        "properties": {
            "symbol": {
                "type": "string",
                "description": "交易品种"
            },
            "m5_data": {
                "type": "string",
                "description": "M5时间框架的Markdown数据（可选）"
            },
            "h1_data": {
                "type": "string",
                "description": "H1时间框架的Markdown数据（可选）"
            },
            "d1_data": {
                "type": "string",
                "description": "D1时间框架的Markdown数据（可选）"
            },
            "analysis_method": {
                "type": "string",
                "description": "分析方法",
                "enum": ["AlBrooks", "PriceAction", "MultiTimeFrame"],
                "default": "AlBrooks"
            }
        },
        "required": ["symbol"]
    }
    """)
)
```

**工具4: calculate_position_size**
```csharp
ChatTool.CreateFunctionTool(
    functionName: "calculate_position_size",
    functionDescription: """
        根据风控规则计算合适的仓位大小。
        支持FTMO、Blue Guardian等Prop Firm规则。
        会检查单日亏损限额和总亏损限额。
        返回是否允许开仓、建议仓位、风险金额等。
        """,
    functionParameters: BinaryData.FromString("""
    {
        "type": "object",
        "properties": {
            "symbol": {
                "type": "string",
                "description": "交易品种，如XAUUSD"
            },
            "broker": {
                "type": "string",
                "description": "经纪商名称，用于获取品种规格",
                "enum": ["ICMarkets", "OandaV20", "BlueGuardian"],
                "default": "ICMarkets"
            },
            "entry_price": {
                "type": "number",
                "description": "入场价格"
            },
            "stop_loss": {
                "type": "number",
                "description": "止损价格"
            },
            "account_balance": {
                "type": "number",
                "description": "当前账户余额"
            },
            "prop_firm_rule": {
                "type": "string",
                "description": "使用的Prop Firm规则",
                "enum": ["FTMO", "BlueGuardian", "Custom"],
                "default": "FTMO"
            },
            "risk_percent": {
                "type": "number",
                "description": "单笔风险百分比，默认1.0%",
                "default": 1.0
            }
        },
        "required": ["symbol", "entry_price", "stop_loss", "account_balance"]
    }
    """)
)
```

**工具5: place_market_order**
```csharp
ChatTool.CreateFunctionTool(
    functionName: "place_market_order",
    functionDescription: """
        在交易平台上执行市价单开仓。
        会自动使用配置的交易平台（Oanda或TradeLocker）。
        返回订单执行结果，包括订单ID、成交价格等。
        注意：这是真实交易，请谨慎使用！
        """,
    functionParameters: BinaryData.FromString("""
    {
        "type": "object",
        "properties": {
            "symbol": {
                "type": "string",
                "description": "交易品种"
            },
            "lots": {
                "type": "number",
                "description": "交易手数"
            },
            "direction": {
                "type": "string",
                "description": "交易方向",
                "enum": ["buy", "sell"]
            },
            "stop_loss": {
                "type": "number",
                "description": "止损价格（可选）"
            },
            "take_profit": {
                "type": "number",
                "description": "止盈价格（可选）"
            },
            "comment": {
                "type": "string",
                "description": "订单备注（可选）"
            }
        },
        "required": ["symbol", "lots", "direction"]
    }
    """)
)
```

**工具6: save_analysis_to_database**
```csharp
ChatTool.CreateFunctionTool(
    functionName: "save_analysis_to_database",
    functionDescription: """
        将AI分析结果保存到Azure Table Storage。
        保存到 AIAnalysisHistory 表中，便于后续查询和回溯。
        """,
    functionParameters: BinaryData.FromString("""
    {
        "type": "object",
        "properties": {
            "symbol": {
                "type": "string",
                "description": "交易品种"
            },
            "analysis_result": {
                "type": "string",
                "description": "分析结果的JSON字符串"
            },
            "timeframe": {
                "type": "string",
                "description": "分析的时间框架"
            }
        },
        "required": ["symbol", "analysis_result"]
    }
    """)
)
```

**工具7: save_trade_decision**
```csharp
ChatTool.CreateFunctionTool(
    functionName: "save_trade_decision",
    functionDescription: """
        将交易决策保存到数据库，包括：
        - AI分析ID
        - 订单ID
        - 仓位大小
        - 入场价格
        - 止损止盈
        - 风控参数
        便于后续跟踪交易表现。
        """,
    functionParameters: BinaryData.FromString("""
    {
        "type": "object",
        "properties": {
            "decision_data": {
                "type": "string",
                "description": "交易决策数据的JSON字符串"
            }
        },
        "required": ["decision_data"]
    }
    """)
)
```

**工具8: get_account_info**
```csharp
ChatTool.CreateFunctionTool(
    functionName: "get_account_info",
    functionDescription: """
        获取当前交易账户信息，包括：
        - 账户余额
        - 当日盈亏
        - 总盈亏
        - 持仓列表
        - 可用保证金
        用于风控计算和决策参考。
        """,
    functionParameters: BinaryData.FromString("""
    {
        "type": "object",
        "properties": {}
    }
    """)
)
```

#### ✅ 4. 数据格式化服务

**文件：** `DataFormatterService.cs`

```csharp
public class DataFormatterService
{
    /// <summary>
    /// 格式化K线数据为Markdown表格
    /// </summary>
    public string FormatToMarkdown(
        List<Candle> candles,
        int emaPeriod = 20,
        bool includeYear = false)
    {
        // 1. 计算EMA指标
        var emaValues = CalculateEMA(candles, emaPeriod);

        // 2. 生成Markdown表格
        var sb = new StringBuilder();
        sb.AppendLine("| Date | Time | Open | High | Low | Close | BodyRange | Body% | EMA20 | Dist_EMA20 |");
        sb.AppendLine("|------|------|------|------|-----|-------|-----------|-------|-------|------------|");

        for (int i = 0; i < candles.Count; i++)
        {
            var c = candles[i];
            var date = includeYear
                ? c.Time.ToString("yyyyMMdd")
                : c.Time.ToString("MMdd");
            var time = c.Time.ToString("HHmm");

            var bodyRange = c.High - c.Low;
            var bodyPercent = bodyRange != 0
                ? (c.Close - c.Open) / bodyRange * 100
                : 0;
            var distEma = i < emaValues.Count
                ? c.Low - emaValues[i]
                : 0;

            sb.AppendLine($"| {date} | {time} | {c.Open:F2} | {c.High:F2} | {c.Low:F2} | {c.Close:F2} | {bodyRange:F2} | {bodyPercent:F1}% | {(i < emaValues.Count ? emaValues[i] : 0):F2} | {distEma:F2} |");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 使用 Skender.Stock.Indicators 计算EMA
    /// </summary>
    private List<decimal> CalculateEMA(List<Candle> candles, int period)
    {
        var quotes = candles.Select(c => new Quote
        {
            Date = c.Time,
            Open = (decimal)c.Open,
            High = (decimal)c.High,
            Low = (decimal)c.Low,
            Close = (decimal)c.Close,
            Volume = (decimal)c.Volume
        }).ToList();

        var emaResults = quotes.GetEma(period);

        return emaResults
            .Select(e => (decimal)(e.Ema ?? 0))
            .ToList();
    }
}
```

#### ✅ 5. API 控制器

**文件：** `AgentController.cs`

```csharp
[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly TradingAgentService _agentService;
    private readonly ILogger<AgentController> _logger;

    public AgentController(
        TradingAgentService agentService,
        ILogger<AgentController> logger)
    {
        _agentService = agentService;
        _logger = logger;
    }

    /// <summary>
    /// 执行AI Agent任务
    /// </summary>
    [HttpPost("execute")]
    [ProducesResponseType(typeof(AgentResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Execute(
        [FromBody] AgentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("收到Agent请求: {Prompt}", request.Prompt);

            var result = await _agentService.ExecutePrompt(
                request.Prompt,
                cancellationToken);

            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Agent任务被取消");
            return StatusCode(499, new { error = "Request cancelled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent执行失败: {Message}", ex.Message);
            return BadRequest(new
            {
                success = false,
                error = ex.Message,
                type = ex.GetType().Name
            });
        }
    }

    /// <summary>
    /// 获取Agent能力列表
    /// </summary>
    [HttpGet("capabilities")]
    public IActionResult GetCapabilities()
    {
        return Ok(new
        {
            tools = new[]
            {
                "get_oanda_candles - 获取K线数据",
                "format_candles_to_markdown - 格式化为Markdown",
                "analyze_market_with_gpt4o - GPT-4o市场分析",
                "calculate_position_size - 计算仓位（FTMO风控）",
                "place_market_order - 执行市价单",
                "save_analysis_to_database - 保存分析结果",
                "save_trade_decision - 保存交易决策",
                "get_account_info - 获取账户信息"
            },
            supported_symbols = new[] { "XAU_USD", "XAG_USD", "EUR_USD", "GBP_USD", "USD_JPY" },
            supported_timeframes = new[] { "M1", "M5", "M15", "M30", "H1", "H4", "D1", "W1", "MN1" },
            risk_rules = new[] { "FTMO", "BlueGuardian", "Custom" }
        });
    }
}
```

**模型定义：**

```csharp
public class AgentRequest
{
    [Required]
    public string Prompt { get; set; } = string.Empty;

    public Dictionary<string, object>? Context { get; set; }
}

public class AgentResponse
{
    public bool Success { get; set; }
    public string Result { get; set; } = string.Empty;
    public List<ExecutionStep> Steps { get; set; } = new();
    public int TotalSteps { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ExecutionStep
{
    public int StepNumber { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Result { get; set; }
    public bool Success { get; set; }
    public TimeSpan Duration { get; set; }
}
```

#### ✅ 6. 配置和服务注册

**appsettings.json:**

```json
{
  "AgentSettings": {
    "Enabled": true,
    "Model": "gpt-4o-mini",
    "MaxIterations": 20,
    "TimeoutSeconds": 300,
    "EnableTracing": true,
    "SafeMode": true,  // true=需要确认才执行真实交易
    "AllowedOperations": [
      "get_data",
      "analyze",
      "calculate_position",
      "save_data"
      // "place_order" 需要明确启用
    ]
  }
}
```

**Program.cs:**

```csharp
// 注册 Agent 服务
builder.Services.Configure<AgentSettings>(
    builder.Configuration.GetSection("AgentSettings"));

builder.Services.AddSingleton<DataFormatterService>();
builder.Services.AddScoped<TradingAgentService>();

// 注册 Controller
builder.Services.AddControllers()
    .AddApplicationPart(typeof(AgentController).Assembly);
```

### 实现步骤

#### 阶段1: 基础框架（1天）

1. **创建项目**
   ```bash
   dotnet new classlib -n Trading.AI.Agent -o src/Trading.AI.Agent
   cd src/Trading.AI.Agent
   dotnet add package Azure.AI.OpenAI --version 2.1.0
   dotnet add package Skender.Stock.Indicators --version 2.7.1
   ```

2. **添加项目引用**
   ```bash
   dotnet add reference ../Trading.AI/Trading.AI.csproj
   dotnet add reference ../Trading.Infras.Data/Trading.Infras.Data.csproj
   dotnet add reference ../Trading.Infras.Service/Trading.Infras.Service.csproj
   dotnet add reference ../Trading.Core/Trading.Core.csproj
   ```

3. **创建基础文件**
   - Models (AgentRequest, AgentResponse)
   - Configuration (AgentSettings)
   - DataFormatterService 基础实现

#### 阶段2: 核心Agent实现（2-3天）

1. **实现工具定义**
   - 定义8个工具的Function Schema
   - 编写清晰的描述和参数说明

2. **实现TradingAgentService**
   - ExecutePrompt 主循环
   - ExecuteFunction 函数路由
   - 8个工具函数的具体实现

3. **实现DataFormatterService**
   - Markdown格式化
   - EMA计算
   - 错误处理

#### 阶段3: API和集成（1天）

1. **实现AgentController**
   - POST /api/agent/execute
   - GET /api/agent/capabilities
   - 错误处理和日志

2. **服务注册和配置**
   - Program.cs 配置
   - appsettings.json
   - User Secrets

#### 阶段4: 测试和文档（1-2天）

1. **编写测试**
   - 单元测试（工具函数）
   - 集成测试（完整流程）
   - 端到端测试（真实场景）

2. **编写文档**
   - API文档
   - 使用示例
   - 故障排查指南

### 测试场景

**测试1: 简单数据获取**
```bash
curl -X POST http://localhost:5000/api/agent/execute \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "获取黄金最新100根5分钟K线"
  }'
```

**测试2: 数据格式化**
```bash
curl -X POST http://localhost:5000/api/agent/execute \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "获取黄金最新50根H1 K线，格式化为Markdown表格，包含EMA20"
  }'
```

**测试3: 市场分析**
```bash
curl -X POST http://localhost:5000/api/agent/execute \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "分析黄金当前市场状态，使用M5和H1数据，给出交易建议"
  }'
```

**测试4: 完整流程（SafeMode）**
```bash
curl -X POST http://localhost:5000/api/agent/execute \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "获取黄金M5最新120根、H1最新80根K线，用GPT-4o分析是否应该开仓，如果要开仓就计算FTMO风控仓位，但不要真的下单，只返回建议"
  }'
```

### 验收标准

**功能完整性：**
- [ ] 8个工具全部实现并测试通过
- [ ] Agent能正确理解简单任务
- [ ] Agent能正确理解复杂任务
- [ ] 工具调用顺序符合逻辑
- [ ] 参数传递正确无误

**数据格式化：**
- [ ] Markdown表格格式正确
- [ ] EMA20计算准确
- [ ] Body%和Dist_EMA20计算正确
- [ ] 日期时间格式符合要求

**错误处理：**
- [ ] API错误有明确提示
- [ ] 工具执行失败能优雅降级
- [ ] 超时处理正确
- [ ] 日志记录完整

**安全性：**
- [ ] SafeMode 正常工作
- [ ] 真实交易需要明确授权
- [ ] API Key 安全存储
- [ ] 敏感信息不记录日志

**性能：**
- [ ] 简单任务 < 10秒
- [ ] 复杂任务 < 60秒
- [ ] 并发请求正常处理
- [ ] 资源占用合理

**文档：**
- [ ] API文档完整
- [ ] 使用示例清晰
- [ ] 配置说明详细
- [ ] 故障排查指南

### 安全考虑

**SafeMode 机制：**
```csharp
if (_settings.SafeMode && toolName == "place_market_order")
{
    _logger.LogWarning("⚠️ SafeMode启用，拒绝真实下单");
    return JsonSerializer.Serialize(new
    {
        success = false,
        message = "SafeMode启用，无法执行真实交易。请在配置中禁用SafeMode或使用模拟模式。",
        simulated_result = "如果执行，将会下单..."
    });
}
```

**操作权限控制：**
```csharp
var allowedOps = _settings.AllowedOperations ?? new List<string>();
if (!allowedOps.Contains(toolName))
{
    return JsonSerializer.Serialize(new
    {
        success = false,
        message = $"操作 {toolName} 未被授权"
    });
}
```

### 未来扩展

**阶段2功能（可选）：**
- [ ] 支持更多交易平台（MT5, cTrader）
- [ ] 支持更多技术指标
- [ ] 支持自定义分析策略
- [ ] 支持语音输入
- [ ] 支持多语言
- [ ] Web UI 界面
- [ ] 实时执行监控
- [ ] 历史任务回溯

### 相关Issue
- 依赖 **Issue 4** (重构)：需要 `IOrderExecutionService` 接口
- 关联 **Issue 2** (Azure OpenAI)：使用已有的 AI 服务

### 标签
`ai`, `agent`, `enhancement`, `openai`, `automation`

---

