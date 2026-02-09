using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Trading.Infrastructure.AI.Configuration;
using Trading.Infrastructure.AI.Models;
using Trading.Models;

namespace Trading.Infrastructure.AI.Services;

/// <summary>
/// 市场分析服务实现
/// </summary>
public class MarketAnalysisService : IMarketAnalysisService
{
    private readonly ILogger<MarketAnalysisService> _logger;
    private readonly IAzureOpenAIService _openAIService;
    private readonly IMemoryCache _cache;
    private readonly MarketAnalysisSettings _settings;
    private readonly Func<string, string, string, string, string, int, long, bool, Task>? _onAnalysisCompleted;

    public MarketAnalysisService(
        ILogger<MarketAnalysisService> logger,
        IAzureOpenAIService openAIService,
        IMemoryCache cache,
        IOptions<MarketAnalysisSettings> settings,
        Func<string, string, string, string, string, int, long, bool, Task>? onAnalysisCompleted = null)
    {
        _logger = logger;
        _openAIService = openAIService;
        _cache = cache;
        _settings = settings.Value;
        _onAnalysisCompleted = onAnalysisCompleted;
    }

    public async Task<Dictionary<string, TrendAnalysis>> AnalyzeMultiTimeFrameTrendAsync(
        string symbol,
        List<string> timeFrames,
        Dictionary<string, List<Candle>> candlesByTimeFrame,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"trend_{symbol}_{string.Join("_", timeFrames)}";

        if (_cache.TryGetValue<Dictionary<string, TrendAnalysis>>(cacheKey, out var cached))
        {
            _logger.LogDebug("使用缓存的趋势分析: {Symbol}", symbol);
            return cached!;
        }

        var systemPrompt = @"你是一位专业的外汇和贵金属交易分析师，擅长多时间框架技术分析。
请基于提供的K线数据，分析每个时间框架的趋势方向和强度。

请以JSON格式返回分析结果，格式如下：
{
  ""H1"": {
    ""direction"": ""Bullish"" | ""Bearish"" | ""Neutral"",
    ""strength"": 0-100,
    ""details"": ""详细分析""
  },
  ""H4"": { ... },
  ""D1"": { ... }
}

分析要点：
1. 判断趋势方向（上升/下降/震荡）
2. 评估趋势强度（考虑均线排列、斜率、ADX等）
3. 识别关键支撑阻力位
4. 注意价格结构（高点低点关系）";

        var userMessage = BuildTrendAnalysisMessage(symbol, timeFrames, candlesByTimeFrame);

        try
        {
            var response = await _openAIService.ChatCompletionAsync(systemPrompt, userMessage, cancellationToken);
            var result = ParseTrendAnalysisResponse(response, timeFrames);

            // 缓存结果
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(_settings.TrendAnalysisCacheMinutes));
            _cache.Set(cacheKey, result, cacheOptions);

            _logger.LogInformation("趋势分析完成: {Symbol}, 时间框架: {TimeFrames}",
                symbol, string.Join(", ", timeFrames));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "趋势分析失败: {Symbol}", symbol);
            throw;
        }
    }

    public async Task<KeyLevelsAnalysis> IdentifyKeyLevelsAsync(
        string symbol,
        List<Candle> candles,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"keylevels_{symbol}";

        if (_cache.TryGetValue<KeyLevelsAnalysis>(cacheKey, out var cached))
        {
            _logger.LogDebug("使用缓存的关键价格位: {Symbol}", symbol);
            return cached!;
        }

        var systemPrompt = @"你是一位专业的价格行为分析师，擅长识别关键支撑和阻力位。
请基于提供的K线数据，识别最重要的支撑和阻力位。

请以JSON格式返回分析结果：
{
  ""supportLevels"": [
    {
      ""price"": 价格,
      ""strength"": 0-100,
      ""touchCount"": 触碰次数,
      ""description"": ""描述""
    }
  ],
  ""resistanceLevels"": [ ... ],
  ""details"": ""整体分析""
}

识别要点：
1. 多次触碰的价格位（3次以上最强）
2. 历史高低点
3. 整数关口（如2000.00）
4. 密集成交区
5. 趋势线和通道线
6. 返回最多5个最重要的支撑和5个阻力";

        var userMessage = BuildKeyLevelsMessage(symbol, candles);

        try
        {
            var response = await _openAIService.ChatCompletionAsync(systemPrompt, userMessage, cancellationToken);
            var result = ParseKeyLevelsResponse(response, symbol);

            // 缓存结果
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(_settings.KeyLevelsCacheMinutes));
            _cache.Set(cacheKey, result, cacheOptions);

            _logger.LogInformation("关键价格位识别完成: {Symbol}, 支撑位: {SupportCount}, 阻力位: {ResistanceCount}",
                symbol, result.SupportLevels.Count, result.ResistanceLevels.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "关键价格位识别失败: {Symbol}", symbol);
            throw;
        }
    }

    public async Task<SignalValidation> ValidatePinBarSignalAsync(
        string symbol,
        Candle pinBar,
        TradeDirection direction,
        Dictionary<string, TrendAnalysis>? trendAnalyses = null,
        KeyLevelsAnalysis? keyLevels = null,
        CancellationToken cancellationToken = default)
    {
        var systemPrompt = @"你是一位专业的交易信号验证专家，擅长评估Pin Bar信号质量。
请综合考虑以下因素验证信号：

1. 多时间框架趋势一致性
2. Pin Bar是否位于关键支撑/阻力位
3. 是否顺应大趋势
4. 价格结构是否支持
5. 风险回报比是否合理

请以JSON格式返回：
{
  ""isValid"": true | false,
  ""qualityScore"": 0-100,
  ""reason"": ""验证原因"",
  ""risk"": ""Low"" | ""Medium"" | ""High"",
  ""recommendation"": ""操作建议"",
  ""details"": ""详细分析""
}

评分标准：
90-100: 极佳信号，强烈推荐
70-89: 良好信号，可以交易
50-69: 一般信号，谨慎交易
0-49: 不建议交易";

        var userMessage = BuildSignalValidationMessage(symbol, pinBar, direction, trendAnalyses, keyLevels);

        try
        {
            var response = await _openAIService.ChatCompletionAsync(systemPrompt, userMessage, cancellationToken);
            var result = ParseSignalValidationResponse(response);

            _logger.LogInformation("信号验证完成: {Symbol} {Direction}, 质量分数: {Score}, 有效: {Valid}",
                symbol, direction, result.QualityScore, result.IsValid);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "信号验证失败: {Symbol}", symbol);
            throw;
        }
    }

    #region 构建消息

    private string BuildTrendAnalysisMessage(
        string symbol,
        List<string> timeFrames,
        Dictionary<string, List<Candle>> candlesByTimeFrame)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"交易品种: {symbol}");
        sb.AppendLine($"分析时间: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();

        foreach (var timeFrame in timeFrames)
        {
            if (!candlesByTimeFrame.TryGetValue(timeFrame, out var candles) || candles.Count == 0)
                continue;

            sb.AppendLine($"## {timeFrame} 时间框架");
            sb.AppendLine($"K线数量: {candles.Count}");

            var recent = candles.TakeLast(20).ToList();
            sb.AppendLine($"最近20根K线摘要:");
            sb.AppendLine($"当前价格: {recent[^1].Close:F5}");
            sb.AppendLine($"最高价: {recent.Max(c => c.High):F5}");
            sb.AppendLine($"最低价: {recent.Min(c => c.Low):F5}");

            // 注：EMA和ADX需要在调用前预先计算
            // if (recent[^1].EMA50 > 0)
            //     sb.AppendLine($"EMA50: {recent[^1].EMA50:F5}");
            // if (recent[^1].EMA200 > 0)
            //     sb.AppendLine($"EMA200: {recent[^1].EMA200:F5}");
            // if (recent[^1].ADX > 0)
            //     sb.AppendLine($"ADX: {recent[^1].ADX:F1}");

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string BuildKeyLevelsMessage(string symbol, List<Candle> candles)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"交易品种: {symbol}");
        sb.AppendLine($"K线数量: {candles.Count}");
        sb.AppendLine($"分析时间: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();

        var currentPrice = candles[^1].Close;
        sb.AppendLine($"当前价格: {currentPrice:F5}");
        sb.AppendLine($"价格范围: {candles.Min(c => c.Low):F5} - {candles.Max(c => c.High):F5}");
        sb.AppendLine();

        // 提供最近100根K线的高低点
        var recent = candles.TakeLast(100).ToList();
        sb.AppendLine("最近100根K线的关键价格点:");

        var highs = recent.Select(c => c.High).OrderByDescending(h => h).Take(10);
        sb.AppendLine($"高点: {string.Join(", ", highs.Select(h => h.ToString("F5")))}");

        var lows = recent.Select(c => c.Low).OrderBy(l => l).Take(10);
        sb.AppendLine($"低点: {string.Join(", ", lows.Select(l => l.ToString("F5")))}");

        return sb.ToString();
    }

    private string BuildSignalValidationMessage(
        string symbol,
        Candle pinBar,
        TradeDirection direction,
        Dictionary<string, TrendAnalysis>? trendAnalyses,
        KeyLevelsAnalysis? keyLevels)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"交易品种: {symbol}");
        sb.AppendLine($"信号类型: Pin Bar {direction}");
        sb.AppendLine($"信号时间: {pinBar.DateTime:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        sb.AppendLine("Pin Bar详情:");
        sb.AppendLine($"开盘: {pinBar.Open:F5}, 收盘: {pinBar.Close:F5}");
        sb.AppendLine($"最高: {pinBar.High:F5}, 最低: {pinBar.Low:F5}");
        var body = Math.Abs(pinBar.Close - pinBar.Open);
        var upperWick = pinBar.High - Math.Max(pinBar.Open, pinBar.Close);
        var lowerWick = Math.Min(pinBar.Open, pinBar.Close) - pinBar.Low;
        sb.AppendLine($"实体: {body:F5}, 上影线: {upperWick:F5}, 下影线: {lowerWick:F5}");

        // 注：ADX需要在调用前预先计算
        // if (pinBar.ADX > 0)
        //     sb.AppendLine($"ADX: {pinBar.ADX:F1}");
        sb.AppendLine();

        if (trendAnalyses != null && trendAnalyses.Any())
        {
            sb.AppendLine("多时间框架趋势:");
            foreach (var (tf, analysis) in trendAnalyses)
            {
                sb.AppendLine($"{tf}: {analysis.Direction} (强度: {analysis.Strength})");
            }
            sb.AppendLine();
        }

        if (keyLevels != null)
        {
            sb.AppendLine($"附近支撑位 (距离当前价格{pinBar.Close:F5}):");
            foreach (var level in keyLevels.SupportLevels.Take(3))
            {
                var distance = pinBar.Close - level.Price;
                sb.AppendLine($"  {level.Price:F5} (距离: {distance:F5}, 强度: {level.Strength})");
            }
            sb.AppendLine();

            sb.AppendLine($"附近阻力位:");
            foreach (var level in keyLevels.ResistanceLevels.Take(3))
            {
                var distance = level.Price - pinBar.Close;
                sb.AppendLine($"  {level.Price:F5} (距离: {distance:F5}, 强度: {level.Strength})");
            }
        }

        return sb.ToString();
    }

    #endregion

    #region 解析响应

    private Dictionary<string, TrendAnalysis> ParseTrendAnalysisResponse(string response, List<string> timeFrames)
    {
        try
        {
            // 提取JSON（移除可能的markdown代码块标记）
            var json = ExtractJson(response);
            var doc = JsonDocument.Parse(json);

            var result = new Dictionary<string, TrendAnalysis>();

            foreach (var timeFrame in timeFrames)
            {
                if (doc.RootElement.TryGetProperty(timeFrame, out var tfElement))
                {
                    var analysis = new TrendAnalysis
                    {
                        TimeFrame = timeFrame,
                        Direction = Enum.Parse<TrendDirection>(
                            tfElement.GetProperty("direction").GetString() ?? "Neutral",
                            ignoreCase: true),
                        Strength = tfElement.GetProperty("strength").GetInt32(),
                        Details = tfElement.GetProperty("details").GetString() ?? "",
                        AnalyzedAt = DateTime.UtcNow
                    };
                    result[timeFrame] = analysis;
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析趋势分析响应失败: {Response}", response);
            throw new InvalidOperationException("无法解析AI响应", ex);
        }
    }

    private KeyLevelsAnalysis ParseKeyLevelsResponse(string response, string symbol)
    {
        try
        {
            var json = ExtractJson(response);
            var doc = JsonDocument.Parse(json);

            var result = new KeyLevelsAnalysis
            {
                Symbol = symbol,
                AnalyzedAt = DateTime.UtcNow,
                Details = doc.RootElement.GetProperty("details").GetString() ?? ""
            };

            if (doc.RootElement.TryGetProperty("supportLevels", out var supports))
            {
                foreach (var item in supports.EnumerateArray())
                {
                    result.SupportLevels.Add(new KeyPriceLevel
                    {
                        Price = item.GetProperty("price").GetDecimal(),
                        Type = LevelType.Support,
                        Strength = item.GetProperty("strength").GetInt32(),
                        TouchCount = item.GetProperty("touchCount").GetInt32(),
                        Description = item.GetProperty("description").GetString() ?? ""
                    });
                }
            }

            if (doc.RootElement.TryGetProperty("resistanceLevels", out var resistances))
            {
                foreach (var item in resistances.EnumerateArray())
                {
                    result.ResistanceLevels.Add(new KeyPriceLevel
                    {
                        Price = item.GetProperty("price").GetDecimal(),
                        Type = LevelType.Resistance,
                        Strength = item.GetProperty("strength").GetInt32(),
                        TouchCount = item.GetProperty("touchCount").GetInt32(),
                        Description = item.GetProperty("description").GetString() ?? ""
                    });
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析关键价格位响应失败: {Response}", response);
            throw new InvalidOperationException("无法解析AI响应", ex);
        }
    }

    private SignalValidation ParseSignalValidationResponse(string response)
    {
        try
        {
            var json = ExtractJson(response);
            var doc = JsonDocument.Parse(json);

            return new SignalValidation
            {
                IsValid = doc.RootElement.GetProperty("isValid").GetBoolean(),
                QualityScore = doc.RootElement.GetProperty("qualityScore").GetInt32(),
                Reason = doc.RootElement.GetProperty("reason").GetString() ?? "",
                Risk = Enum.Parse<RiskLevel>(
                    doc.RootElement.GetProperty("risk").GetString() ?? "Medium",
                    ignoreCase: true),
                Recommendation = doc.RootElement.GetProperty("recommendation").GetString() ?? "",
                Details = doc.RootElement.GetProperty("details").GetString() ?? "",
                ValidatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析信号验证响应失败: {Response}", response);
            throw new InvalidOperationException("无法解析AI响应", ex);
        }
    }

    private string ExtractJson(string response)
    {
        // 移除markdown代码块标记
        response = response.Trim();
        if (response.StartsWith("```json"))
            response = response["```json".Length..];
        else if (response.StartsWith("```"))
            response = response["```".Length..];

        if (response.EndsWith("```"))
            response = response[..^3];

        return response.Trim();
    }

    #endregion
}
