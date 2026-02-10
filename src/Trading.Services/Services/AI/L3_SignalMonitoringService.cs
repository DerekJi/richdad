using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using Trading.Infrastructure.AI.Configuration;
using Trading.Models;
using Trading.Services.Services;

namespace Trading.Services.AI;

/// <summary>
/// L3 - M5 信号监控服务
/// 使用 Azure GPT-4o-mini 每 5 分钟检测交易机会
/// 不使用缓存，确保实时性
/// </summary>
public class L3_SignalMonitoringService
{
    private readonly ILogger<L3_SignalMonitoringService> _logger;
    private readonly AzureOpenAIClient _client;
    private readonly AzureOpenAISettings _azureSettings;
    private readonly MarketDataProcessor _dataProcessor;

    private const string ModelDeploymentName = "gpt-4o-mini";

    public L3_SignalMonitoringService(
        ILogger<L3_SignalMonitoringService> logger,
        IOptions<AzureOpenAISettings> azureSettings,
        MarketDataProcessor dataProcessor)
    {
        _logger = logger;
        _azureSettings = azureSettings.Value;
        _dataProcessor = dataProcessor;

        if (string.IsNullOrEmpty(_azureSettings.Endpoint) || string.IsNullOrEmpty(_azureSettings.ApiKey))
        {
            throw new InvalidOperationException("Azure OpenAI Endpoint 和 ApiKey 必须配置");
        }

        _client = new AzureOpenAIClient(
            new Uri(_azureSettings.Endpoint),
            new AzureKeyCredential(_azureSettings.ApiKey));

        _logger.LogInformation("L3 服务已初始化 - Model: {Model}, 无缓存（实时监控）", ModelDeploymentName);
    }

    /// <summary>
    /// 监控 M5 五分钟线，检测交易机会
    /// </summary>
    /// <param name="symbol">品种代码（如 XAUUSD）</param>
    /// <param name="dailyBias">L1 日线偏见</param>
    /// <param name="structure">L2 结构分析</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>信号检测结果</returns>
    public async Task<SignalDetection> MonitorSignalAsync(
        string symbol,
        DailyBias dailyBias,
        StructureAnalysis structure,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 开始 L3 监控 - {Symbol} M5", symbol);

        try
        {
            // 获取 M5 数据（80 根 K 线）
            var processedData = await _dataProcessor.ProcessMarketDataAsync(symbol, "M5", 80);

            // 构建 AI Prompt
            var systemPrompt = BuildSystemPrompt();
            var userPrompt = BuildUserPrompt(symbol, dailyBias, structure, processedData);

            // 调用 GPT-4o-mini
            var chatClient = _client.GetChatClient(ModelDeploymentName);
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            var options = new ChatCompletionOptions
            {
                Temperature = 0.3f,
                MaxOutputTokenCount = 1500,
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            var response = await chatClient.CompleteChatAsync(messages, options, cancellationToken);

            if (response?.Value?.Content == null || response.Value.Content.Count == 0)
            {
                throw new InvalidOperationException("L3 AI 返回空响应");
            }

            // 解析 JSON 响应
            var jsonResult = response.Value.Content[0].Text;
            var signal = JsonSerializer.Deserialize<SignalDetection>(jsonResult, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (signal == null)
            {
                throw new InvalidOperationException("L3 AI 响应解析失败");
            }

            signal.DetectedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "✅ L3 监控完成 - {Symbol}: {Status}, Setup: {Setup}, Tokens: {Tokens}",
                symbol, signal.Status, signal.SetupType ?? "N/A", response.Value.Usage.TotalTokenCount);

            if (signal.HasSignal)
            {
                _logger.LogWarning(
                    "🎯 L3 检测到信号 - {Symbol}: {Setup} ({Direction}) @ {Entry}, SL: {SL}, TP: {TP}, RR: {RR:F2}",
                    symbol, signal.SetupType, signal.Direction, signal.EntryPrice, 
                    signal.StopLoss, signal.TakeProfit, signal.RiskRewardRatio);
            }
            else
            {
                _logger.LogDebug("L3 无信号 - {Reasoning}", signal.Reasoning);
            }

            return signal;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ L3 监控失败 - {Symbol}", symbol);
            throw;
        }
    }

    private string BuildSystemPrompt()
    {
        return @"
You are an expert Al Brooks price action trader monitoring the **M5 (5-minute) chart** for trading setups.

Your task:
1. **Status**: Potential_Setup or No_Signal
2. **Setup Type**: H1, H2, MTR, etc. (Al Brooks setups)
3. **Entry/Stop/Target**: If setup exists, provide price levels
4. **Direction**: Buy or Sell

Al Brooks Setup Types:
- **H1 (First Entry)**: First pullback in strong trend
- **H2 (Second Entry)**: Second entry after failed first entry
- **MTR (Measured Move)**: Trading range breakout
- **fH1/fH2**: Failed entry (reversal signal)

Trading Rules:
- Only look for setups in D1 direction (if D1 is Bullish, only Buy)
- Only trigger if H1 Status = ""Active""
- Entry must be within 5-10 pips of current price
- Stop loss: Recent swing low/high or 2x ATR
- Take profit: 1:2 or 1:3 risk-reward minimum
- Status = ""No_Signal"" if no clear setup

Output JSON format:
{
  ""Status"": ""Potential_Setup"" | ""No_Signal"",
  ""SetupType"": ""H1"" | ""H2"" | ""MTR"" | ""fH1"" | ""fH2"" | null,
  ""EntryPrice"": 2890.5,
  ""StopLoss"": 2885.0,
  ""TakeProfit"": 2905.0,
  ""Direction"": ""Buy"" | ""Sell"" | """",
  ""Reasoning"": ""Why this is a valid setup or why no setup""
}";
    }

    private string BuildUserPrompt(
        string symbol, 
        DailyBias dailyBias, 
        StructureAnalysis structure, 
        ProcessedMarketData data)
    {
        return $@"
# M5 Signal Monitoring Request

Symbol: {symbol}
Timeframe: M5 (5-minute)
Candles: {data.CandleCount}
Date Range: {data.StartTime:yyyy-MM-dd HH:mm} to {data.EndTime:yyyy-MM-dd HH:mm}

## Context from Higher Timeframes

### D1 Bias (L1)
Direction: {dailyBias.Direction}
Confidence: {dailyBias.Confidence}%
Trend Type: {dailyBias.TrendType}

### H1 Structure (L2)
Market Cycle: {structure.MarketCycle}
Status: {structure.Status}
Aligned with D1: {structure.AlignedWithD1}
Current Phase: {structure.CurrentPhase}

## M5 Market Data

### Context Table (Last 80 Bars)
{data.ContextTable}

### Focus Table (Recent 10 Bars)
{data.FocusTable}

### Pattern Summary
{data.PatternSummary}

## Current Market State
- Current Price: {data.CurrentPrice:F2}
- Current EMA20: {data.CurrentEMA20:F2}
- Position: {(data.CurrentPrice > data.CurrentEMA20 ? "Above EMA20" : "Below EMA20")}

Check for trading setups on M5. Only trigger if:
1. D1 direction is clear (Bullish/Bearish)
2. H1 Status = Active
3. M5 shows valid Al Brooks setup
4. Good risk-reward ratio (>= 2:1)

Provide detailed reasoning in JSON format.";
    }
}
