using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Trading.AI.Models;
using Trading.AI.Services;
using Trading.Infras.Data.Models;
using Trading.Infras.Data.Repositories;
using Trading.Data.Models;

namespace Trading.Infras.Service.Services;

/// <summary>
/// 带持久化功能的市场分析服务包装器
/// </summary>
public class MarketAnalysisServiceWithPersistence : IMarketAnalysisService
{
    private readonly IMarketAnalysisService _innerService;
    private readonly IAIAnalysisRepository _repository;
    private readonly ILogger<MarketAnalysisServiceWithPersistence> _logger;

    public MarketAnalysisServiceWithPersistence(
        IMarketAnalysisService innerService,
        IAIAnalysisRepository repository,
        ILogger<MarketAnalysisServiceWithPersistence> logger)
    {
        _innerService = innerService;
        _repository = repository;
        _logger = logger;
    }

    public async Task<Dictionary<string, TrendAnalysis>> AnalyzeMultiTimeFrameTrendAsync(
        string symbol,
        List<string> timeFrames,
        Dictionary<string, List<Candle>> candlesByTimeFrame,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var inputData = new
        {
            symbol,
            timeFrames,
            candleCounts = candlesByTimeFrame.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Count
            )
        };

        try
        {
            var result = await _innerService.AnalyzeMultiTimeFrameTrendAsync(
                symbol, timeFrames, candlesByTimeFrame, cancellationToken);

            stopwatch.Stop();

            // 保存成功的分析记录
            await SaveAnalysisAsync(
                analysisType: "TrendAnalysis",
                symbol: symbol,
                timeFrame: string.Join(",", timeFrames),
                inputData: JsonSerializer.Serialize(inputData),
                rawResponse: "", // 内部服务不暴露原始响应
                parsedResult: JsonSerializer.Serialize(result),
                isSuccess: true,
                estimatedTokens: EstimateTokens(inputData, result),
                responseTimeMs: stopwatch.ElapsedMilliseconds,
                fromCache: false // 缓存逻辑在内部服务
            );

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // 保存失败的分析记录
            await SaveAnalysisAsync(
                analysisType: "TrendAnalysis",
                symbol: symbol,
                timeFrame: string.Join(",", timeFrames),
                inputData: JsonSerializer.Serialize(inputData),
                rawResponse: "",
                parsedResult: "",
                isSuccess: false,
                estimatedTokens: 0,
                responseTimeMs: stopwatch.ElapsedMilliseconds,
                fromCache: false,
                errorMessage: ex.Message
            );

            throw;
        }
    }

    public async Task<KeyLevelsAnalysis> IdentifyKeyLevelsAsync(
        string symbol,
        List<Candle> candles,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var inputData = new
        {
            symbol,
            candleCount = candles.Count,
            priceRange = new
            {
                high = candles.Max(c => c.High),
                low = candles.Min(c => c.Low)
            }
        };

        try
        {
            var result = await _innerService.IdentifyKeyLevelsAsync(symbol, candles, cancellationToken);
            stopwatch.Stop();

            await SaveAnalysisAsync(
                analysisType: "KeyLevels",
                symbol: symbol,
                timeFrame: null,
                inputData: JsonSerializer.Serialize(inputData),
                rawResponse: "",
                parsedResult: JsonSerializer.Serialize(result),
                isSuccess: true,
                estimatedTokens: EstimateTokens(inputData, result),
                responseTimeMs: stopwatch.ElapsedMilliseconds,
                fromCache: false
            );

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            await SaveAnalysisAsync(
                analysisType: "KeyLevels",
                symbol: symbol,
                timeFrame: null,
                inputData: JsonSerializer.Serialize(inputData),
                rawResponse: "",
                parsedResult: "",
                isSuccess: false,
                estimatedTokens: 0,
                responseTimeMs: stopwatch.ElapsedMilliseconds,
                fromCache: false,
                errorMessage: ex.Message
            );

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
        var stopwatch = Stopwatch.StartNew();
        var inputData = new
        {
            symbol,
            direction = direction.ToString(),
            pinBar = new
            {
                pinBar.DateTime,
                pinBar.Open,
                pinBar.High,
                pinBar.Low,
                pinBar.Close
            },
            hasTrendAnalyses = trendAnalyses != null,
            hasKeyLevels = keyLevels != null
        };

        try
        {
            var result = await _innerService.ValidatePinBarSignalAsync(
                symbol, pinBar, direction, trendAnalyses, keyLevels, cancellationToken);
            stopwatch.Stop();

            await SaveAnalysisAsync(
                analysisType: "SignalValidation",
                symbol: symbol,
                timeFrame: null,
                inputData: JsonSerializer.Serialize(inputData),
                rawResponse: "",
                parsedResult: JsonSerializer.Serialize(result),
                isSuccess: true,
                estimatedTokens: EstimateTokens(inputData, result),
                responseTimeMs: stopwatch.ElapsedMilliseconds,
                fromCache: false
            );

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            await SaveAnalysisAsync(
                analysisType: "SignalValidation",
                symbol: symbol,
                timeFrame: null,
                inputData: JsonSerializer.Serialize(inputData),
                rawResponse: "",
                parsedResult: "",
                isSuccess: false,
                estimatedTokens: 0,
                responseTimeMs: stopwatch.ElapsedMilliseconds,
                fromCache: false,
                errorMessage: ex.Message
            );

            throw;
        }
    }

    private async Task SaveAnalysisAsync(
        string analysisType,
        string symbol,
        string? timeFrame,
        string inputData,
        string rawResponse,
        string parsedResult,
        bool isSuccess,
        int estimatedTokens,
        long responseTimeMs,
        bool fromCache,
        string? errorMessage = null)
    {
        try
        {
            var analysis = new AIAnalysisHistory
            {
                AnalysisType = analysisType,
                Symbol = symbol,
                TimeFrame = timeFrame,
                InputData = inputData,
                RawResponse = rawResponse,
                ParsedResult = parsedResult,
                IsSuccess = isSuccess,
                EstimatedTokens = estimatedTokens,
                ResponseTimeMs = responseTimeMs,
                FromCache = fromCache,
                ErrorMessage = errorMessage
            };

            await _repository.SaveAnalysisAsync(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存AI分析记录失败，但不影响主流程");
        }
    }

    private static int EstimateTokens(object input, object output)
    {
        // 简单估算：每4个字符约等于1个token
        var inputJson = JsonSerializer.Serialize(input);
        var outputJson = JsonSerializer.Serialize(output);
        return (inputJson.Length + outputJson.Length) / 4;
    }
}
