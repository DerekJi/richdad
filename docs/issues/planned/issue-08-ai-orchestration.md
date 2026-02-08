## Issue 8: 实现四级 AI 决策编排系统

### 标题
🤖 Implement Four-Tier AI Decision Orchestration System with Multi-Model Integration

### 描述
实现基于 Al Brooks 理论的四级 AI 决策编排系统，通过多模型协作（Azure GPT-4o + DeepSeek）实现从宏观分析到微观决策的完整交易流程。

### 背景
单一 AI 模型难以同时处理宏观趋势分析和微观入场时机判断。通过分级架构：
- **L1 (D1 战略层)**：确定日内交易方向偏见
- **L2 (H1 结构层)**：判断市场周期（趋势/震荡）
- **L3 (M5 监控层)**：识别潜在交易机会
- **L4 (决策层)**：最终开仓决策（带思维链推理）

每一级使用最适合的模型：
- **Azure GPT-4o**：宏观分析（L1）、信号识别（L3）
- **Azure GPT-4o-mini**：高频监控（L3）
- **DeepSeek-V3**：结构分析（L2）
- **DeepSeek-R1**：最终决策（L4，带 CoT 思维链）

### 架构设计

```
┌─────────────────────────────────────────────────────────┐
│  L1: D1 Strategic Analysis (GPT-4o)                     │
│  → Determine daily bias: Bullish/Bearish/Neutral        │
│  → Identify support/resistance levels                    │
│  → Output: Daily trading bias                           │
└────────────────────┬────────────────────────────────────┘
                     ↓ (If trend clear)
┌─────────────────────────────────────────────────────────┐
│  L2: H1 Structure Analysis (DeepSeek-V3)                │
│  → Analyze market cycle: Trend/Channel/Range            │
│  → Check alignment with D1 bias                         │
│  → Output: Active/Idle status                           │
└────────────────────┬────────────────────────────────────┘
                     ↓ (If Active)
┌─────────────────────────────────────────────────────────┐
│  L3: M5 Signal Monitoring (GPT-4o-mini)                 │
│  → Every 5 minutes, check for setups                    │
│  → Filter out low-probability signals                   │
│  → Output: Potential_Setup / No_Signal                  │
└────────────────────┬────────────────────────────────────┘
                     ↓ (If Potential_Setup)
┌─────────────────────────────────────────────────────────┐
│  L4: Final Decision (DeepSeek-R1 with CoT)              │
│  → Receive context from L1/L2/L3                        │
│  → Apply Al Brooks theory critically                    │
│  → Think: "Why should I NOT trade?"                     │
│  → Output: Execute/Reject with reasoning                │
└─────────────────────────────────────────────────────────┘
```

### 实现功能

#### ✅ 1. 基础模型

**决策上下文模型：**

```csharp
public class TradingContext
{
    // L1 输出
    public DailyBias L1_DailyBias { get; set; } = new();

    // L2 输出
    public StructureAnalysis L2_Structure { get; set; } = new();

    // L3 输出
    public SignalDetection L3_Signal { get; set; } = new();

    // 原始数据
    public ProcessedMarketData MarketData { get; set; } = new();

    // 时间戳
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class DailyBias
{
    public string Direction { get; set; } = "Neutral"; // Bullish/Bearish/Neutral
    public double Confidence { get; set; } // 0-100
    public List<double> SupportLevels { get; set; } = new();
    public List<double> ResistanceLevels { get; set; } = new();
    public string TrendType { get; set; } = ""; // Strong/Weak/Sideways
    public string Reasoning { get; set; } = "";
    public DateTime AnalyzedAt { get; set; }
}

public class StructureAnalysis
{
    public string MarketCycle { get; set; } = ""; // Trend/Channel/Range
    public string Status { get; set; } = "Idle"; // Active/Idle
    public bool AlignedWithD1 { get; set; }
    public string CurrentPhase { get; set; } = ""; // Breakout/Pullback/Trading Range
    public string Reasoning { get; set; } = "";
    public DateTime AnalyzedAt { get; set; }
}

public class SignalDetection
{
    public string Status { get; set; } = "No_Signal"; // Potential_Setup/No_Signal
    public string SetupType { get; set; } = ""; // H2/L2/MTR/Gap_Bar
    public double EntryPrice { get; set; }
    public double StopLoss { get; set; }
    public double TakeProfit { get; set; }
    public string Direction { get; set; } = ""; // Buy/Sell
    public string Reasoning { get; set; } = "";
    public DateTime DetectedAt { get; set; }
}

public class FinalDecision
{
    public string Action { get; set; } = "Reject"; // Execute/Reject
    public string Direction { get; set; } = "";
    public double EntryPrice { get; set; }
    public double StopLoss { get; set; }
    public double TakeProfit { get; set; }
    public double LotSize { get; set; }
    public string Reasoning { get; set; } = "";
    public string ThinkingProcess { get; set; } = ""; // DeepSeek-R1 的思维链
    public int ConfidenceScore { get; set; } // 0-100
    public List<string> RiskFactors { get; set; } = new();
    public DateTime DecidedAt { get; set; }
}
```

#### ✅ 2. L1 - 日线战略分析

**新增服务：** `L1_DailyAnalysisService`

```csharp
public class L1_DailyAnalysisService
{
    private readonly AzureOpenAIClient _aiClient;
    private readonly MarketDataProcessor _dataProcessor;
    private readonly ILogger<L1_DailyAnalysisService> _logger;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// 分析 D1 日线，确定当日交易偏见
    /// 每天 UTC+2 00:00 执行一次，结果缓存 24 小时
    /// </summary>
    public async Task<DailyBias> AnalyzeDailyBiasAsync(string symbol)
    {
        var cacheKey = $"L1_DailyBias_{symbol}_{DateTime.UtcNow:yyyyMMdd}";

        // 检查缓存
        if (_cache.TryGetValue<DailyBias>(cacheKey, out var cachedBias))
        {
            _logger.LogInformation("从缓存返回 D1 分析结果");
            return cachedBias;
        }

        // 获取 D1 数据（80 根足够）
        var processedData = await _dataProcessor.ProcessMarketDataAsync(symbol, "D1", 80);

        // 构建 System Prompt
        var systemPrompt = @"
You are Al Brooks, a master of Price Action trading.

Your task: Analyze the D1 (daily) chart and provide a **trading bias** for today.

Focus on:
1. **Trend Direction**: Is this a strong bull trend, bear trend, or trading range?
2. **Market Phase**: Breakout, pullback, or consolidation?
3. **Key Levels**: Identify major support/resistance from recent swing highs/lows.
4. **Today's Bias**: Should traders look for longs, shorts, or stay flat?

Output format (JSON):
{
  ""Direction"": ""Bullish"" | ""Bearish"" | ""Neutral"",
  ""Confidence"": 0-100,
  ""SupportLevels"": [price1, price2],
  ""ResistanceLevels"": [price1, price2],
  ""TrendType"": ""Strong"" | ""Weak"" | ""Sideways"",
  ""Reasoning"": ""Brief explanation based on Al Brooks theory""
}";

        // 构建 User Prompt
        var userPrompt = $@"
# Market Context
Symbol: {symbol}
Timeframe: D1
Current Date: {DateTime.UtcNow:yyyy-MM-dd}

{processedData.ContextTable}

{processedData.FocusTable}

{processedData.PatternSummary}

Analyze and provide today's trading bias.";

        // 调用 GPT-4o
        var chatOptions = new ChatCompletionOptions
        {
            Temperature = 0.3f,
            MaxTokens = 1000,
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
        };

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        };

        var completion = await _aiClient.GetChatClient("gpt-4o")
            .CompleteChatAsync(messages, chatOptions);

        var response = completion.Value.Content[0].Text;
        var bias = JsonSerializer.Deserialize<DailyBias>(response);
        bias.AnalyzedAt = DateTime.UtcNow;

        // 缓存 24 小时
        _cache.Set(cacheKey, bias, TimeSpan.FromHours(24));

        _logger.LogInformation(
            "L1 分析完成: {Direction} (信心: {Confidence}%)",
            bias.Direction, bias.Confidence);

        return bias;
    }
}
```

#### ✅ 3. L2 - 小时结构分析

**新增服务：** `L2_StructureAnalysisService`

```csharp
public class L2_StructureAnalysisService
{
    private readonly HttpClient _deepSeekClient;
    private readonly MarketDataProcessor _dataProcessor;
    private readonly ILogger<L2_StructureAnalysisService> _logger;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// 分析 H1 结构，判断市场周期
    /// 每小时执行一次，结果缓存 1 小时
    /// </summary>
    public async Task<StructureAnalysis> AnalyzeStructureAsync(
        string symbol,
        DailyBias dailyBias)
    {
        var cacheKey = $"L2_Structure_{symbol}_{DateTime.UtcNow:yyyyMMddHH}";

        if (_cache.TryGetValue<StructureAnalysis>(cacheKey, out var cachedStructure))
        {
            _logger.LogInformation("从缓存返回 H1 结构分析");
            return cachedStructure;
        }

        // 获取 H1 数据（120 根）
        var processedData = await _dataProcessor.ProcessMarketDataAsync(symbol, "H1", 120);

        // 构建 Prompt
        var systemPrompt = @"
You are analyzing the H1 (1-hour) chart to determine the market structure.

Given the D1 bias, your job is to decide:
1. **Market Cycle**: Is this a trending market, a channel, or a trading range?
2. **Status**: Should we be actively looking for trades (Active) or wait (Idle)?
3. **Alignment**: Does H1 align with the D1 bias?

Rules:
- If D1 is Bullish, we only look for long setups on H1 pullbacks.
- If H1 is in a tight trading range, Status = Idle.
- If H1 shows a clear trend in D1 direction, Status = Active.

Output JSON:
{
  ""MarketCycle"": ""Trend"" | ""Channel"" | ""Range"",
  ""Status"": ""Active"" | ""Idle"",
  ""AlignedWithD1"": true | false,
  ""CurrentPhase"": ""Breakout"" | ""Pullback"" | ""Trading Range"",
  ""Reasoning"": ""Explanation""
}";

        var userPrompt = $@"
# D1 Bias (from L1)
Direction: {dailyBias.Direction}
Confidence: {dailyBias.Confidence}%
Reasoning: {dailyBias.Reasoning}

# H1 Market Data
Symbol: {symbol}
Timeframe: H1

{processedData.ContextTable}

{processedData.FocusTable}

{processedData.PatternSummary}

Analyze H1 structure and decide Status.";

        // 调用 DeepSeek-V3
        var requestBody = new
        {
            model = "deepseek-chat",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.3,
            response_format = new { type = "json_object" }
        };

        var response = await _deepSeekClient.PostAsJsonAsync("", requestBody);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonDocument.Parse(responseContent);

        var structure = JsonSerializer.Deserialize<StructureAnalysis>(
            result.RootElement.GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content").GetString());

        structure.AnalyzedAt = DateTime.UtcNow;

        // 缓存 1 小时
        _cache.Set(cacheKey, structure, TimeSpan.FromHours(1));

        _logger.LogInformation(
            "L2 分析完成: {MarketCycle}, Status={Status}",
            structure.MarketCycle, structure.Status);

        return structure;
    }
}
```

#### ✅ 4. L3 - 5分钟信号监控

**新增服务：** `L3_SignalMonitoringService`

```csharp
public class L3_SignalMonitoringService
{
    private readonly AzureOpenAIClient _aiClient;
    private readonly MarketDataProcessor _dataProcessor;
    private readonly ILogger<L3_SignalMonitoringService> _logger;

    /// <summary>
    /// 监控 M5 图表，寻找交易设置
    /// 每 5 分钟执行一次（当 L2 Status = Active 时）
    /// </summary>
    public async Task<SignalDetection> MonitorForSignalsAsync(
        string symbol,
        TradingContext context)
    {
        // 仅在 L2 Status = Active 时执行
        if (context.L2_Structure.Status != "Active")
        {
            return new SignalDetection
            {
                Status = "No_Signal",
                Reasoning = "L2 Status is Idle, no monitoring needed"
            };
        }

        // 获取 M5 数据（最近 80 根）
        var processedData = await _dataProcessor.ProcessMarketDataAsync(symbol, "M5", 80);

        // 使用 GPT-4o-mini（成本低）
        var systemPrompt = @"
You are monitoring the M5 chart for Al Brooks Price Action setups.

Given:
- D1 Bias (from L1)
- H1 Structure (from L2)
- M5 Recent bars

Your task: Identify if there is a **potential trading setup**.

Al Brooks Setups to look for:
1. **H2/L2** (Second entry in trend)
2. **MTR** (Major Trend Reversal at key level)
3. **Gap Bar** (EMA20 gap with strong momentum)
4. **ii Breakout** (Inside-inside structure breakout)

If found, provide entry, stop loss, take profit based on signal bar.

Output JSON:
{
  ""Status"": ""Potential_Setup"" | ""No_Signal"",
  ""SetupType"": ""H2"" | ""L2"" | ""MTR"" | ""Gap_Bar"" | """",
  ""EntryPrice"": 0.0,
  ""StopLoss"": 0.0,
  ""TakeProfit"": 0.0,
  ""Direction"": ""Buy"" | ""Sell"" | """",
  ""Reasoning"": ""Brief explanation""
}";

        var userPrompt = $@"
# Trading Context

## L1 - D1 Bias
Direction: {context.L1_DailyBias.Direction}
Key Levels: Support={string.Join(", ", context.L1_DailyBias.SupportLevels)},
            Resistance={string.Join(", ", context.L1_DailyBias.ResistanceLevels)}

## L2 - H1 Structure
Market Cycle: {context.L2_Structure.MarketCycle}
Current Phase: {context.L2_Structure.CurrentPhase}

## M5 - Recent Bars
Symbol: {symbol}

{processedData.FocusTable}

{processedData.PatternSummary}

Check for trading setups. Remember: We only trade in the direction of D1 bias.
If D1 is Bullish, only look for long setups.";

        var chatOptions = new ChatCompletionOptions
        {
            Temperature = 0.3f,
            MaxTokens = 800,
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
        };

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        };

        var completion = await _aiClient.GetChatClient("gpt-4o-mini")
            .CompleteChatAsync(messages, chatOptions);

        var response = completion.Value.Content[0].Text;
        var signal = JsonSerializer.Deserialize<SignalDetection>(response);
        signal.DetectedAt = DateTime.UtcNow;

        _logger.LogInformation(
            "L3 监控完成: {Status}, Setup={SetupType}",
            signal.Status, signal.SetupType);

        return signal;
    }
}
```

#### ✅ 5. L4 - 最终决策（带思维链）

**新增服务：** `L4_FinalDecisionService`

```csharp
public class L4_FinalDecisionService
{
    private readonly HttpClient _deepSeekClient;
    private readonly ILogger<L4_FinalDecisionService> _logger;

    /// <summary>
    /// 最终决策：使用 DeepSeek-R1 进行深度推理
    /// 仅在 L3 检测到 Potential_Setup 时触发
    /// </summary>
    public async Task<FinalDecision> MakeFinalDecisionAsync(
        TradingContext context)
    {
        // 仅在 L3 发现潜在设置时执行
        if (context.L3_Signal.Status != "Potential_Setup")
        {
            return new FinalDecision
            {
                Action = "Reject",
                Reasoning = "No potential setup from L3"
            };
        }

        // 构建 System Prompt（批判性思维模式）
        var systemPrompt = @"
You are Al Brooks. You are about to make a real trading decision with real money.

Your PRIMARY job is to find reasons NOT to trade. You are a professional skeptic.

Given:
- D1 daily bias
- H1 structure analysis
- M5 signal detection (with suggested entry/SL/TP)

Your analysis process:
1. **Check Alignment**: Does everything align? D1/H1/M5?
2. **Risk Assessment**: Is this really a high-probability setup?
3. **Find Flaws**: What could go wrong? Is this a trap?
4. **Final Call**: Execute or Reject?

IMPORTANT:
- If there is ANY doubt, choose Reject.
- FTMO requires 60%+ win rate. Only take the BEST setups.
- Consider: Is the stop loss too wide? Is TP realistic? Is momentum fading?

Output JSON:
{
  ""Action"": ""Execute"" | ""Reject"",
  ""Direction"": ""Buy"" | ""Sell"" | """",
  ""EntryPrice"": 0.0,
  ""StopLoss"": 0.0,
  ""TakeProfit"": 0.0,
  ""LotSize"": 0.0,
  ""Reasoning"": ""Your final conclusion"",
  ""ThinkingProcess"": ""Your step-by-step reasoning (Chain of Thought)"",
  ""ConfidenceScore"": 0-100,
  ""RiskFactors"": [""factor1"", ""factor2""]
}";

        var userPrompt = $@"
# Complete Trading Context

## L1 - D1 Daily Bias
Direction: {context.L1_DailyBias.Direction}
Confidence: {context.L1_DailyBias.Confidence}%
Trend Type: {context.L1_DailyBias.TrendType}
Support Levels: {string.Join(", ", context.L1_DailyBias.SupportLevels)}
Resistance Levels: {string.Join(", ", context.L1_DailyBias.ResistanceLevels)}
L1 Reasoning: {context.L1_DailyBias.Reasoning}

## L2 - H1 Structure
Market Cycle: {context.L2_Structure.MarketCycle}
Status: {context.L2_Structure.Status}
Aligned with D1: {context.L2_Structure.AlignedWithD1}
Current Phase: {context.L2_Structure.CurrentPhase}
L2 Reasoning: {context.L2_Structure.Reasoning}

## L3 - M5 Signal Detection
Setup Type: {context.L3_Signal.SetupType}
Suggested Entry: {context.L3_Signal.EntryPrice}
Suggested Stop Loss: {context.L3_Signal.StopLoss}
Suggested Take Profit: {context.L3_Signal.TakeProfit}
Direction: {context.L3_Signal.Direction}
L3 Reasoning: {context.L3_Signal.Reasoning}

## M5 Market Data (Focus Table - Last 30 Bars)
{context.MarketData.FocusTable}

## Pattern Summary
{context.MarketData.PatternSummary}

---

Now, apply your critical thinking. Should we execute this trade or reject it?
Think step by step, and provide your Chain of Thought in the ThinkingProcess field.";

        // 调用 DeepSeek-R1（支持思维链）
        var requestBody = new
        {
            model = "deepseek-reasoner",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.5,
            max_tokens = 16000
        };

        var response = await _deepSeekClient.PostAsJsonAsync("", requestBody);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonDocument.Parse(responseContent);

        var choice = result.RootElement.GetProperty("choices")[0];
        var message = choice.GetProperty("message");

        // DeepSeek-R1 返回的思维过程在 reasoning_content 字段
        var thinkingProcess = message.GetProperty("reasoning_content").GetString();
        var finalAnswer = message.GetProperty("content").GetString();

        var decision = JsonSerializer.Deserialize<FinalDecision>(finalAnswer);
        decision.ThinkingProcess = thinkingProcess;
        decision.DecidedAt = DateTime.UtcNow;

        _logger.LogInformation(
            "L4 最终决策: {Action} (信心: {Confidence}%)",
            decision.Action, decision.ConfidenceScore);

        _logger.LogInformation("思维过程: {ThinkingProcess}",
            thinkingProcess?.Substring(0, Math.Min(200, thinkingProcess.Length)));

        return decision;
    }
}
```

#### ✅ 6. 编排服务（总控）

**新增服务：** `TradingOrchestrationService`

```csharp
public class TradingOrchestrationService
{
    private readonly L1_DailyAnalysisService _l1Service;
    private readonly L2_StructureAnalysisService _l2Service;
    private readonly L3_SignalMonitoringService _l3Service;
    private readonly L4_FinalDecisionService _l4Service;
    private readonly ILogger<TradingOrchestrationService> _logger;

    /// <summary>
    /// 执行完整的四级决策流程
    /// </summary>
    public async Task<FinalDecision> ExecuteTradingPipelineAsync(string symbol)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("开始四级 AI 决策流程: {Symbol}", symbol);

        try
        {
            // L1: 日线分析
            _logger.LogInformation("执行 L1 - D1 战略分析...");
            var dailyBias = await _l1Service.AnalyzeDailyBiasAsync(symbol);

            // 如果日线不明确，直接拒绝
            if (dailyBias.Direction == "Neutral" || dailyBias.Confidence < 60)
            {
                _logger.LogWarning("L1 方向不明确或信心不足，终止流程");
                return new FinalDecision
                {
                    Action = "Reject",
                    Reasoning = "D1 bias is unclear or low confidence"
                };
            }

            // L2: 小时结构分析
            _logger.LogInformation("执行 L2 - H1 结构分析...");
            var structure = await _l2Service.AnalyzeStructureAsync(symbol, dailyBias);

            // 如果 H1 状态为 Idle，不继续
            if (structure.Status == "Idle")
            {
                _logger.LogInformation("L2 Status=Idle，暂无交易机会");
                return new FinalDecision
                {
                    Action = "Reject",
                    Reasoning = "H1 market structure is not favorable (Idle)"
                };
            }

            // L3: M5 信号监控
            _logger.LogInformation("执行 L3 - M5 信号监控...");
            var context = new TradingContext
            {
                L1_DailyBias = dailyBias,
                L2_Structure = structure,
                MarketData = await _dataProcessor.ProcessMarketDataAsync(symbol, "M5", 80)
            };

            var signal = await _l3Service.MonitorForSignalsAsync(symbol, context);
            context.L3_Signal = signal;

            // 如果没有信号，不继续
            if (signal.Status != "Potential_Setup")
            {
                _logger.LogInformation("L3 未检测到交易设置");
                return new FinalDecision
                {
                    Action = "Reject",
                    Reasoning = "No trading setup detected on M5"
                };
            }

            // L4: 最终决策（DeepSeek-R1 思维链）
            _logger.LogInformation("执行 L4 - 最终决策（DeepSeek-R1）...");
            var decision = await _l4Service.MakeFinalDecisionAsync(context);

            stopwatch.Stop();
            _logger.LogInformation(
                "四级决策完成: {Action}, 耗时 {ElapsedMs}ms",
                decision.Action, stopwatch.ElapsedMilliseconds);

            return decision;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "四级决策流程发生错误");
            return new FinalDecision
            {
                Action = "Reject",
                Reasoning = $"System error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 定时任务：每 5 分钟执行一次 M5 监控
    /// </summary>
    public async Task RunPeriodicMonitoringAsync(string symbol)
    {
        while (true)
        {
            try
            {
                var decision = await ExecuteTradingPipelineAsync(symbol);

                // 如果决策是 Execute，发送 Telegram 通知
                if (decision.Action == "Execute")
                {
                    await SendTelegramNotificationAsync(symbol, decision);
                }

                // 等待 5 分钟
                await Task.Delay(TimeSpan.FromMinutes(5));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "定时监控任务错误");
                await Task.Delay(TimeSpan.FromMinutes(1)); // 错误后等待 1 分钟重试
            }
        }
    }
}
```

### 配置管理

**appsettings.json:**

```json
{
  "AIOrchestration": {
    "EnabledLevels": ["L1", "L2", "L3", "L4"],
    "L1": {
      "Model": "gpt-4o",
      "CacheDurationHours": 24,
      "MinConfidence": 60
    },
    "L2": {
      "Model": "deepseek-chat",
      "CacheDurationHours": 1
    },
    "L3": {
      "Model": "gpt-4o-mini",
      "MonitoringIntervalMinutes": 5
    },
    "L4": {
      "Model": "deepseek-reasoner",
      "MinConfidenceToExecute": 75,
      "MaxThinkingTokens": 16000
    }
  },
  "DeepSeek": {
    "ApiKey": "",
    "BaseUrl": "https://api.deepseek.com/v1/chat/completions"
  }
}
```

### 验收标准

**功能完整性：**
- [ ] L1 正确分析 D1 趋势
- [ ] L2 正确判断 H1 结构
- [ ] L3 能识别 Al Brooks 设置
- [ ] L4 提供完整思维链推理
- [ ] 四级级联逻辑正确

**上下文传递：**
- [ ] 下级能接收上级结论
- [ ] 条件触发正常工作
- [ ] 早期终止逻辑正确

**性能和成本：**
- [ ] L1 分析 < 10秒
- [ ] L2 分析 < 5秒
- [ ] L3 监控 < 3秒
- [ ] L4 决策 < 30秒
- [ ] 日总成本 < $1

**缓存机制：**
- [ ] L1 结果缓存 24 小时
- [ ] L2 结果缓存 1 小时
- [ ] 缓存失效正常工作

### 相关文件

**新增文件：**
- `Trading.AI/Services/L1_DailyAnalysisService.cs`
- `Trading.AI/Services/L2_StructureAnalysisService.cs`
- `Trading.AI/Services/L3_SignalMonitoringService.cs`
- `Trading.AI/Services/L4_FinalDecisionService.cs`
- `Trading.AI/Services/TradingOrchestrationService.cs`
- `Trading.AI/Models/TradingContext.cs`

**文档：**
- `docs/FOUR_TIER_AI_ARCHITECTURE.md` - 架构详解
- `docs/AI_PROMPTS.md` - Prompt 模板

### 标签
`ai`, `enhancement`, `orchestration`, `multi-model`, `decision-making`

---

