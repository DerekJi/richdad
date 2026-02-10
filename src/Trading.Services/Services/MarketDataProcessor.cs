using Microsoft.Extensions.Logging;
using Trading.Infrastructure.Models;
using Trading.Infrastructure.Repositories;
using Trading.Models;
using Trading.Services.Utilities;

namespace Trading.Services.Services;

/// <summary>
/// 市场数据处理服务 - 整合 K 线、指标、形态识别，为 AI Prompt 准备数据
/// </summary>
/// <remarks>
/// 核心职责：
/// 1. 获取原始 K 线数据（通过 CandleCacheService）
/// 2. 计算技术指标（EMA20、Body%、Range 等）
/// 3. 识别 Al Brooks 形态（Inside、ii、Breakout 等）
/// 4. 生成 Markdown 表格（供 AI 分析）
/// 5. 返回 ProcessedMarketData（包含完整上下文）
/// </remarks>
public class MarketDataProcessor
{
    private readonly CandleCacheService _cacheService;
    private readonly TechnicalIndicatorService _indicatorService;
    private readonly PatternRecognitionService _patternService;
    private readonly MarkdownTableGenerator _tableGenerator;
    private readonly IProcessedDataRepository _repository;
    private readonly ILogger<MarketDataProcessor> _logger;

    public MarketDataProcessor(
        CandleCacheService cacheService,
        TechnicalIndicatorService indicatorService,
        PatternRecognitionService patternService,
        MarkdownTableGenerator tableGenerator,
        IProcessedDataRepository repository,
        ILogger<MarketDataProcessor> logger)
    {
        _cacheService = cacheService;
        _indicatorService = indicatorService;
        _patternService = patternService;
        _tableGenerator = tableGenerator;
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// 完整的数据处理管道 - 一站式获取 AI Prompt 所需的所有数据
    /// </summary>
    /// <param name="symbol">品种代码（如 XAUUSD）</param>
    /// <param name="timeFrame">时间周期（如 M5、H1、D1）</param>
    /// <param name="count">K 线数量（建议：D1=80, H1=120, M5=80）</param>
    /// <param name="useCache">是否优先使用预处理缓存数据</param>
    /// <returns>处理后的市场数据（包含 Markdown 表格）</returns>
    public async Task<ProcessedMarketData> ProcessMarketDataAsync(
        string symbol,
        string timeFrame,
        int count,
        bool useCache = true)
    {
        _logger.LogInformation(
            "开始处理市场数据: {Symbol} {TimeFrame}, 请求 {Count} 根 K 线",
            symbol, timeFrame, count);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // 步骤 1: 优先尝试从预处理缓存获取数据（如果启用）
            List<ProcessedDataEntity>? cachedProcessedData = null;
            if (useCache)
            {
                cachedProcessedData = await TryGetCachedProcessedDataAsync(symbol, timeFrame, count);
            }

            ProcessedMarketData result;

            if (cachedProcessedData != null && cachedProcessedData.Any())
            {
                // 使用缓存的预处理数据
                _logger.LogInformation("使用预处理缓存数据，共 {Count} 根", cachedProcessedData.Count);
                result = BuildFromCachedData(cachedProcessedData, symbol, timeFrame);
            }
            else
            {
                // 完整处理流程
                result = await ProcessFromRawDataAsync(symbol, timeFrame, count);
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "市场数据处理完成: {Symbol} {TimeFrame}, 耗时 {ElapsedMs}ms, K 线数量 {Count}",
                symbol, timeFrame, stopwatch.ElapsedMilliseconds, result.CandleCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "市场数据处理失败: {Symbol} {TimeFrame}", symbol, timeFrame);
            throw;
        }
    }

    /// <summary>
    /// 从原始数据完整处理（当缓存不可用时）
    /// </summary>
    private async Task<ProcessedMarketData> ProcessFromRawDataAsync(
        string symbol,
        string timeFrame,
        int count)
    {
        _logger.LogInformation("从原始数据进行完整处理");

        // 1. 获取原始 K 线数据
        var candles = await _cacheService.GetCandlesAsync(symbol, timeFrame, count);

        if (!candles.Any())
        {
            _logger.LogWarning("未获取到 K 线数据，返回空结果");
            return CreateEmptyResult(symbol, timeFrame);
        }

        _logger.LogInformation("获取到 {Count} 根 K 线", candles.Count);

        // 2. 计算 EMA20
        var ema20Values = CalculateEMAArray(candles, 20);

        // 3. 计算衍生指标并识别形态
        var processedDataList = new List<ProcessedDataEntity>();
        var patternsByIndex = new Dictionary<int, List<string>>();

        for (int i = 0; i < candles.Count; i++)
        {
            var candle = candles[i];
            var ema20 = ema20Values[i];

            // 计算指标
            var bodyPercent = _indicatorService.CalculateBodyPercent(candle);
            var closePos = _indicatorService.CalculateClosePosition(candle);
            var distEMA = _indicatorService.CalculateDistanceToEMA(candle.Close, ema20, symbol);
            var range = _indicatorService.CalculateRange(candle);
            var bodySizePercent = _indicatorService.CalculateBodySizePercent(candle);
            var upperTail = _indicatorService.CalculateUpperTailPercent(candle);
            var lowerTail = _indicatorService.CalculateLowerTailPercent(candle);

            // 识别形态（需要传入当前索引之前的所有 K 线）
            var candlesUpToNow = candles.Take(i + 1).ToList();
            var tags = _patternService.RecognizePatterns(
                candlesUpToNow, i, ema20, symbol);

            patternsByIndex[i] = tags;

            // 判断是否为信号棒（简单实现：有形态标签且实体足够大）
            var isSignalBar = tags.Any() && bodySizePercent > 0.4;

            // 构建 ProcessedDataEntity
            var processedData = new ProcessedDataEntity
            {
                Symbol = symbol,
                TimeFrame = timeFrame,
                Time = candle.DateTime,
                BodyPercent = bodyPercent,
                ClosePosition = closePos,
                DistanceToEMA20 = distEMA,
                Range = (double)range,
                BodySizePercent = bodySizePercent,
                UpperTailPercent = upperTail,
                LowerTailPercent = lowerTail,
                EMA20 = (double)ema20,
                Tags = System.Text.Json.JsonSerializer.Serialize(tags),
                IsSignalBar = isSignalBar,
                Open = (double)candle.Open,
                High = (double)candle.High,
                Low = (double)candle.Low,
                Close = (double)candle.Close,
                // 设置 Table Storage 必需字段
                PartitionKey = $"{symbol}_{timeFrame}",
                RowKey = candle.DateTime.ToString("yyyyMMdd_HHmm"),
                Timestamp = DateTimeOffset.UtcNow,
                ETag = new Azure.ETag("*")
            };

            processedDataList.Add(processedData);
        }

        _logger.LogInformation("完成 {Count} 根 K 线的形态识别", processedDataList.Count);

        // 4. 生成 Markdown 表格
        var contextTable = _tableGenerator.GenerateFullTable(processedDataList, symbol, timeFrame, includeHeader: true);
        var focusData = processedDataList.TakeLast(30).ToList();
        var focusTable = _tableGenerator.GenerateCompactTable(focusData, includeHeader: true);
        var patternSummary = _tableGenerator.GeneratePatternSummary(processedDataList);

        // 5. 构建返回结果
        var result = new ProcessedMarketData
        {
            Symbol = symbol,
            TimeFrame = timeFrame,
            ContextTable = contextTable,
            FocusTable = focusTable,
            PatternSummary = patternSummary,
            RawCandles = candles,
            EMA20Values = ema20Values,
            PatternsByIndex = patternsByIndex
        };

        return result;
    }

    /// <summary>
    /// 从缓存的预处理数据构建结果
    /// </summary>
    private ProcessedMarketData BuildFromCachedData(
        List<ProcessedDataEntity> cachedData,
        string symbol,
        string timeFrame)
    {
        // 生成 Markdown 表格
        var contextTable = _tableGenerator.GenerateFullTable(cachedData, symbol, timeFrame, includeHeader: true);
        var focusData = cachedData.TakeLast(30).ToList();
        var focusTable = _tableGenerator.GenerateCompactTable(focusData, includeHeader: true);
        var patternSummary = _tableGenerator.GeneratePatternSummary(cachedData);

        // 重建 RawCandles 和 EMA20Values（从 ProcessedDataEntity 提取）
        var rawCandles = cachedData.Select(d => new Candle
        {
            DateTime = d.Time,
            Open = (decimal)d.Open,
            High = (decimal)d.High,
            Low = (decimal)d.Low,
            Close = (decimal)d.Close,
            IsComplete = true
        }).ToList();

        var ema20Values = cachedData.Select(d => (decimal)d.EMA20).ToArray();

        // 重建 PatternsByIndex
        var patternsByIndex = new Dictionary<int, List<string>>();
        for (int i = 0; i < cachedData.Count; i++)
        {
            patternsByIndex[i] = cachedData[i].GetTags();
        }

        return new ProcessedMarketData
        {
            Symbol = symbol,
            TimeFrame = timeFrame,
            ContextTable = contextTable,
            FocusTable = focusTable,
            PatternSummary = patternSummary,
            RawCandles = rawCandles,
            EMA20Values = ema20Values,
            PatternsByIndex = patternsByIndex
        };
    }

    /// <summary>
    /// 尝试从预处理缓存获取数据
    /// </summary>
    private async Task<List<ProcessedDataEntity>?> TryGetCachedProcessedDataAsync(
        string symbol,
        string timeFrame,
        int count)
    {
        try
        {
            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddDays(-7); // 查询最近 7 天

            var entities = await _repository.GetRangeAsync(symbol, timeFrame, startTime, endTime);
            var recentData = entities
                .OrderByDescending(e => e.Time)
                .Take(count)
                .OrderBy(e => e.Time)
                .ToList();

            if (recentData.Count >= count * 0.9) // 至少有 90% 的数据
            {
                _logger.LogInformation("从预处理缓存获取 {Count} 根数据", recentData.Count);
                return recentData;
            }

            _logger.LogInformation("预处理缓存数据不足（{Count}/{Required}），将进行完整处理",
                recentData.Count, count);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "从预处理缓存获取数据失败，将进行完整处理");
            return null;
        }
    }

    /// <summary>
    /// 计算 EMA20 数组
    /// </summary>
    private decimal[] CalculateEMAArray(List<Candle> candles, int period)
    {
        if (candles.Count < period)
            return Array.Empty<decimal>();

        var emaValues = new decimal[candles.Count];

        // 使用 SMA 作为初始值
        var sma = candles.Take(period).Average(c => c.Close);
        emaValues[period - 1] = sma;

        var multiplier = 2.0m / (period + 1);

        for (int i = period; i < candles.Count; i++)
        {
            emaValues[i] = (candles[i].Close - emaValues[i - 1]) * multiplier + emaValues[i - 1];
        }

        // 填充前面的值（使用第一个有效值）
        for (int i = 0; i < period - 1; i++)
        {
            emaValues[i] = emaValues[period - 1];
        }

        return emaValues;
    }

    /// <summary>
    /// 创建空结果（当没有数据时）
    /// </summary>
    private ProcessedMarketData CreateEmptyResult(string symbol, string timeFrame)
    {
        return new ProcessedMarketData
        {
            Symbol = symbol,
            TimeFrame = timeFrame,
            ContextTable = "No data available.",
            FocusTable = "No data available.",
            PatternSummary = "No data available.",
            RawCandles = new List<Candle>(),
            EMA20Values = Array.Empty<decimal>(),
            PatternsByIndex = new Dictionary<int, List<string>>()
        };
    }
}
