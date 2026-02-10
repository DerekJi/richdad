using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Trading.Infrastructure.AI.Services;
using Trading.Models;
using Trading.Services.Services;

namespace Trading.Services.AI;

/// <summary>
/// L2 - H1 结构层分析服务
/// 使用 DeepSeek-V3 分析小时线结构，判断市场周期
/// 结果缓存 1 小时
/// </summary>
public class L2_StructureAnalysisService
{
    private readonly ILogger<L2_StructureAnalysisService> _logger;
    private readonly IDeepSeekService _deepSeekService;
    private readonly MarketDataProcessor _dataProcessor;
    private readonly IMemoryCache _cache;

    private const int CacheHours = 1;

    public L2_StructureAnalysisService(
        ILogger<L2_StructureAnalysisService> logger,
        IDeepSeekService deepSeekService,
        MarketDataProcessor dataProcessor,
        IMemoryCache cache)
    {
        _logger = logger;
        _deepSeekService = deepSeekService;
        _dataProcessor = dataProcessor;
        _cache = cache;

        _logger.LogInformation("L2 服务已初始化 - Model: DeepSeek-V3, Cache: {Hours}h", CacheHours);
    }

    /// <summary>
    /// 分析 H1 小时线，判断市场结构和周期
    /// </summary>
    /// <param name="symbol">品种代码（如 XAUUSD）</param>
    /// <param name="dailyBias">L1 日线偏见</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结构分析结果</returns>
    public async Task<StructureAnalysis> AnalyzeStructureAsync(
        string symbol,
        DailyBias dailyBias,
        CancellationToken cancellationToken = default)
    {
        // 生成缓存键（每小时一次）
        var cacheKey = $"L2_Structure_{symbol}_{DateTime.UtcNow:yyyyMMddHH}";

        // 检查缓存
        if (_cache.TryGetValue<StructureAnalysis>(cacheKey, out var cachedStructure))
        {
            _logger.LogInformation("✅ 从缓存返回 L2 分析 - {Symbol}", symbol);
            return cachedStructure!;
        }

        _logger.LogInformation("🔍 开始 L2 分析 - {Symbol} H1", symbol);

        try
        {
            // 获取 H1 数据（120 根 K 线）
            var processedData = await _dataProcessor.ProcessMarketDataAsync(symbol, "H1", 120);

            // 构建 AI Prompt
            var systemPrompt = BuildSystemPrompt();
            var userPrompt = BuildUserPrompt(symbol, dailyBias, processedData);

            // 调用 DeepSeek-V3
            var response = await _deepSeekService.ChatCompletionAsync(
                systemPrompt,
                userPrompt,
                cancellationToken);

            // 解析 JSON 响应
            var structure = JsonSerializer.Deserialize<StructureAnalysis>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (structure == null)
            {
                throw new InvalidOperationException("L2 AI 响应解析失败");
            }

            structure.AnalyzedAt = DateTime.UtcNow;

            // 缓存结果（1 小时）
            _cache.Set(cacheKey, structure, TimeSpan.FromHours(CacheHours));

            _logger.LogInformation(
                "✅ L2 分析完成 - {Symbol}: {Cycle} ({Status}), 与 D1 对齐: {Aligned}, 缓存 {Hours}h",
                symbol, structure.MarketCycle, structure.Status, structure.AlignedWithD1, CacheHours);

            _logger.LogDebug("L2 推理: {Reasoning}", structure.Reasoning);

            return structure;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ L2 分析失败 - {Symbol}", symbol);
            throw;
        }
    }

    private string BuildSystemPrompt()
    {
        return @"
You are an expert Al Brooks price action trader analyzing the **H1 (1-hour) chart** to determine market structure.

Your task:
1. **Market Cycle**: Is this a Trend, Channel, or Range?
2. **Status**: Should we actively look for trades (Active) or wait (Idle)?
3. **Alignment**: Does H1 align with D1 bias?
4. **Current Phase**: Breakout, Pullback, or Trading Range?

Al Brooks Principles:
- **Trend**: Clear swing highs/lows, most closes above/below EMA20
- **Channel**: Moving in a channel with pullbacks to trendline
- **Range**: Oscillating between support and resistance
- **Active**: Clear structure, aligned with D1, tradeable setups
- **Idle**: Choppy, unclear, or against D1 bias

Trading Rules:
- If D1 is Bullish, only look for long setups on H1 pullbacks
- If D1 is Bearish, only look for short setups on H1 rallies
- If H1 is in tight trading range, Status = Idle (wait for breakout)
- If H1 shows clear trend in D1 direction, Status = Active

Output JSON format:
{
  ""MarketCycle"": ""Trend"" | ""Channel"" | ""Range"",
  ""Status"": ""Active"" | ""Idle"",
  ""AlignedWithD1"": true | false,
  ""CurrentPhase"": ""Breakout"" | ""Pullback"" | ""Trading Range"",
  ""Reasoning"": ""Brief explanation why Active or Idle""
}";
    }

    private string BuildUserPrompt(string symbol, DailyBias dailyBias, ProcessedMarketData data)
    {
        return $@"
# H1 Structure Analysis Request

Symbol: {symbol}
Timeframe: H1 (1-hour)
Candles: {data.CandleCount}
Date Range: {data.StartTime:yyyy-MM-dd HH:mm} to {data.EndTime:yyyy-MM-dd HH:mm}

## D1 Bias (from L1)
Direction: {dailyBias.Direction}
Confidence: {dailyBias.Confidence}%
Trend Type: {dailyBias.TrendType}
Reasoning: {dailyBias.Reasoning}

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

Analyze H1 structure considering D1 bias. Decide if we should be Active (looking for trades) or Idle (waiting).";
    }

    /// <summary>
    /// 清除特定品种的缓存
    /// </summary>
    public void ClearCache(string symbol)
    {
        var cacheKey = $"L2_Structure_{symbol}_{DateTime.UtcNow:yyyyMMddHH}";
        _cache.Remove(cacheKey);
        _logger.LogInformation("🗑️ L2 缓存已清除 - {Symbol}", symbol);
    }
}
