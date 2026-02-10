using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using Trading.Infrastructure.AI.Configuration;
using Trading.Models;
using Trading.Services.Services;

namespace Trading.Services.AI;

/// <summary>
/// L1 - D1 战略层分析服务
/// 使用 Azure GPT-4o 分析日线数据，确定交易方向偏见
/// 结果缓存 24 小时
/// </summary>
public class L1_DailyAnalysisService
{
    private readonly ILogger<L1_DailyAnalysisService> _logger;
    private readonly AzureOpenAIClient _client;
    private readonly AzureOpenAISettings _azureSettings;
    private readonly MarketDataProcessor _dataProcessor;
    private readonly IMemoryCache _cache;

    private const string ModelDeploymentName = "gpt-4o";
    private const int CacheHours = 24;

    public L1_DailyAnalysisService(
        ILogger<L1_DailyAnalysisService> logger,
        IOptions<AzureOpenAISettings> azureSettings,
        MarketDataProcessor dataProcessor,
        IMemoryCache cache)
    {
        _logger = logger;
        _azureSettings = azureSettings.Value;
        _dataProcessor = dataProcessor;
        _cache = cache;

        if (string.IsNullOrEmpty(_azureSettings.Endpoint) || string.IsNullOrEmpty(_azureSettings.ApiKey))
        {
            throw new InvalidOperationException("Azure OpenAI Endpoint 和 ApiKey 必须配置");
        }

        _client = new AzureOpenAIClient(
            new Uri(_azureSettings.Endpoint),
            new AzureKeyCredential(_azureSettings.ApiKey));

        _logger.LogInformation("L1 服务已初始化 - Model: {Model}, Cache: {Hours}h", 
            ModelDeploymentName, CacheHours);
    }

    /// <summary>
    /// 分析 D1 日线，确定交易方向偏见
    /// </summary>
    /// <param name="symbol">品种代码（如 XAUUSD）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>日线偏见分析结果</returns>
    public async Task<DailyBias> AnalyzeDailyBiasAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        // 生成缓存键（每日一次）
        var cacheKey = $"L1_DailyBias_{symbol}_{DateTime.UtcNow:yyyyMMdd}";

        // 检查缓存
        if (_cache.TryGetValue<DailyBias>(cacheKey, out var cachedBias))
        {
            _logger.LogInformation("✅ 从缓存返回 L1 分析 - {Symbol}", symbol);
            return cachedBias!;
        }

        _logger.LogInformation("🔍 开始 L1 分析 - {Symbol} D1", symbol);

        try
        {
            // 获取 D1 数据（80 根 K 线）
            var processedData = await _dataProcessor.ProcessMarketDataAsync(symbol, "D1", 80);

            // 构建 AI Prompt
            var systemPrompt = BuildSystemPrompt();
            var userPrompt = BuildUserPrompt(symbol, processedData);

            // 调用 GPT-4o
            var chatClient = _client.GetChatClient(ModelDeploymentName);
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            var options = new ChatCompletionOptions
            {
                Temperature = 0.3f,
                MaxOutputTokenCount = 1000,
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            var response = await chatClient.CompleteChatAsync(messages, options, cancellationToken);

            if (response?.Value?.Content == null || response.Value.Content.Count == 0)
            {
                throw new InvalidOperationException("L1 AI 返回空响应");
            }

            // 解析 JSON 响应
            var jsonResult = response.Value.Content[0].Text;
            var bias = JsonSerializer.Deserialize<DailyBias>(jsonResult, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (bias == null)
            {
                throw new InvalidOperationException("L1 AI 响应解析失败");
            }

            bias.AnalyzedAt = DateTime.UtcNow;

            // 缓存结果（24 小时）
            _cache.Set(cacheKey, bias, TimeSpan.FromHours(CacheHours));

            _logger.LogInformation(
                "✅ L1 分析完成 - {Symbol}: {Direction} (置信度: {Confidence}%), 缓存 {Hours}h",
                symbol, bias.Direction, bias.Confidence, CacheHours);

            _logger.LogDebug(
                "L1 推理: {Reasoning}, Tokens: {Tokens}",
                bias.Reasoning, response.Value.Usage.TotalTokenCount);

            return bias;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ L1 分析失败 - {Symbol}", symbol);
            throw;
        }
    }

    private string BuildSystemPrompt()
    {
        return @"
You are an expert Al Brooks price action trader analyzing the **D1 (Daily) chart** to determine the trading bias for the day.

Your task:
1. **Determine Direction**: Is the market Bullish, Bearish, or Neutral?
2. **Confidence Level**: How confident are you? (0-100)
3. **Identify Key Levels**: Support and resistance levels
4. **Trend Type**: Strong/Weak/Sideways

Al Brooks Principles to Apply:
- Look for **consecutive bull/bear bars** (strong trends)
- Identify **trading ranges** (neutral markets)
- Check if the market is in a **broad channel** (bull or bear channel)
- Look for **climax patterns** (reversal signals)
- Consider **pullbacks to EMA20** (trend continuation)

Rules:
- Only say ""Bullish"" if you see a clear bull trend (most bars closing above EMA20)
- Only say ""Bearish"" if you see a clear bear trend (most bars closing below EMA20)
- Say ""Neutral"" if in a trading range or weak trend
- Confidence >= 75 means strong conviction
- Confidence < 60 means weak/uncertain

Output JSON format:
{
  ""Direction"": ""Bullish"" | ""Bearish"" | ""Neutral"",
  ""Confidence"": 0-100,
  ""SupportLevels"": [2850.0, 2870.5],
  ""ResistanceLevels"": [2920.0, 2950.0],
  ""TrendType"": ""Strong"" | ""Weak"" | ""Sideways"",
  ""Reasoning"": ""Brief explanation using Al Brooks terminology""
}";
    }

    private string BuildUserPrompt(string symbol, ProcessedMarketData data)
    {
        return $@"
# Daily Chart Analysis Request

Symbol: {symbol}
Timeframe: D1 (Daily)
Candles: {data.CandleCount}
Date Range: {data.StartTime:yyyy-MM-dd} to {data.EndTime:yyyy-MM-dd}

## Context Table (Last 80 Bars)
{data.ContextTable}

## Focus Table (Recent 10 Bars)
{data.FocusTable}

## Pattern Summary
{data.PatternSummary}

## Current Market State
- Current Price: {data.CurrentPrice:F2}
- Current EMA20: {data.CurrentEMA20:F2}
- Position: {(data.CurrentPrice > data.CurrentEMA20 ? "Above EMA20" : "Below EMA20")}

Based on the above D1 data, analyze and provide your daily trading bias in JSON format.";
    }

    /// <summary>
    /// 清除特定品种的缓存
    /// </summary>
    public void ClearCache(string symbol)
    {
        var cacheKey = $"L1_DailyBias_{symbol}_{DateTime.UtcNow:yyyyMMdd}";
        _cache.Remove(cacheKey);
        _logger.LogInformation("🗑️ L1 缓存已清除 - {Symbol}", symbol);
    }
}
