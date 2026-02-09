using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Trading.Infrastructure.Configuration;
using Trading.Infrastructure.Models;
using Trading.Infrastructure.Repositories;
using Trading.Infrastructure.Services;
using Trading.Models;

namespace Trading.Services.Services;

/// <summary>
/// 数据初始化服务 - 用于首次运行时填充历史数据
/// </summary>
public class CandleInitializationService
{
    private readonly IOandaService _oandaService;
    private readonly ICandleRepository _repository;
    private readonly IProcessedDataRepository _processedDataRepository;
    private readonly TechnicalIndicatorService _indicatorService;
    private readonly PatternRecognitionService _patternService;
    private readonly ILogger<CandleInitializationService> _logger;
    private readonly CandleCacheSettings _settings;

    public CandleInitializationService(
        IOandaService oandaService,
        ICandleRepository repository,
        IProcessedDataRepository processedDataRepository,
        TechnicalIndicatorService indicatorService,
        PatternRecognitionService patternService,
        ILogger<CandleInitializationService> logger,
        IOptions<CandleCacheSettings> settings)
    {
        _oandaService = oandaService;
        _repository = repository;
        _processedDataRepository = processedDataRepository;
        _indicatorService = indicatorService;
        _patternService = patternService;
        _logger = logger;
        _settings = settings.Value;
    }

    /// <summary>
    /// 初始化历史数据（首次运行）
    /// </summary>
    /// <param name="symbols">品种列表（如果为空则使用配置中的列表）</param>
    /// <param name="timeFrames">时间周期列表（如果为空则使用配置中的列表）</param>
    /// <param name="count">K线数量（如果为null则使用默认值）</param>
    public async Task InitializeHistoricalDataAsync(
        List<string>? symbols = null,
        List<string>? timeFrames = null,
        int? count = null)
    {
        symbols ??= _settings.PreloadSymbols;
        timeFrames ??= _settings.PreloadTimeFrames;

        _logger.LogInformation(
            "开始初始化历史数据：{SymbolCount} 个品种，{TimeFrameCount} 个周期",
            symbols.Count, timeFrames.Count);

        var totalTasks = symbols.Count * timeFrames.Count;
        var completedTasks = 0;

        foreach (var symbol in symbols)
        {
            foreach (var timeFrame in timeFrames)
            {
                try
                {
                    await InitializeSymbolTimeFrameAsync(symbol, timeFrame, count);
                    completedTasks++;

                    _logger.LogInformation(
                        "进度：{Completed}/{Total} ({Percent}%)",
                        completedTasks, totalTasks, (completedTasks * 100 / totalTasks));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "初始化 {Symbol} {TimeFrame} 失败",
                        symbol, timeFrame);
                }
            }
        }

        _logger.LogInformation("历史数据初始化完成");
    }

    /// <summary>
    /// 初始化单个品种和时间周期的数据
    /// </summary>
    private async Task InitializeSymbolTimeFrameAsync(string symbol, string timeFrame, int? count = null)
    {
        // 检查是否已有数据
        var latestTime = await _repository.GetLatestTimeAsync(symbol, timeFrame);

        if (latestTime != null)
        {
            _logger.LogInformation(
                "{Symbol} {TimeFrame} 已有数据，最新时间：{LatestTime}，跳过初始化",
                symbol, timeFrame, latestTime);
            return;
        }

        // 根据时间周期确定获取的 K 线数量
        count ??= GetInitialDataCount(timeFrame);
        var candleCount = count.Value; // 此时count肯定有值

        _logger.LogInformation(
            "正在初始化 {Symbol} {TimeFrame} 数据，共 {Count} 根...",
            symbol, timeFrame, candleCount);

        try
        {
            var serviceCandles = await _oandaService.GetHistoricalDataAsync(symbol, timeFrame, candleCount);
            var candles = serviceCandles;

            if (candles.Any())
            {
                await _repository.SaveBatchAsync(symbol, timeFrame, candles);

                _logger.LogInformation(
                    "成功初始化 {Symbol} {TimeFrame}：{Count} 根 K 线，时间范围：{Start} - {End}",
                    symbol, timeFrame, candles.Count,
                    candles.First().DateTime, candles.Last().DateTime);

                // 执行形态识别并保存预处理数据
                await ProcessAndSavePatternDataAsync(symbol, timeFrame, candles);
            }
            else
            {
                _logger.LogWarning(
                    "{Symbol} {TimeFrame} 未获取到任何数据",
                    symbol, timeFrame);
            }

            // 避免 API 速率限制
            await Task.Delay(1000);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "从 OANDA 获取 {Symbol} {TimeFrame} 数据失败",
                symbol, timeFrame);
            throw;
        }
    }

    /// <summary>
    /// 增量更新数据（补充最新数据）
    /// </summary>
    /// <param name="symbol">品种代码</param>
    /// <param name="timeFrame">时间周期</param>
    public async Task IncrementalUpdateAsync(string symbol, string timeFrame)
    {
        try
        {
            var latestTime = await _repository.GetLatestTimeAsync(symbol, timeFrame);

            if (latestTime == null)
            {
                _logger.LogInformation(
                    "{Symbol} {TimeFrame} 没有历史数据，执行完整初始化",
                    symbol, timeFrame);
                await InitializeSymbolTimeFrameAsync(symbol, timeFrame, count: null);
                return;
            }

            var now = DateTime.UtcNow;
            var timeDiff = now - latestTime.Value;

            // 如果最新数据很新（在一个周期内），则不需要更新
            var periodMinutes = GetTimeFrameMinutes(timeFrame);

            _logger.LogInformation(
                "增量更新检查：{Symbol} {TimeFrame}，最新时间: {LatestTime}，当前时间: {Now}，时间差: {Diff}分钟",
                symbol, timeFrame, latestTime.Value, now, timeDiff.TotalMinutes);

            if (timeDiff.TotalMinutes < periodMinutes)
            {
                _logger.LogInformation(
                    "{Symbol} {TimeFrame} 数据已是最新（时间差{Diff}分钟 < 周期{Period}分钟），无需更新",
                    symbol, timeFrame, timeDiff.TotalMinutes, periodMinutes);
                return;
            }

            // 计算需要补充的数据量
            var requiredCount = (int)Math.Ceiling(timeDiff.TotalMinutes / periodMinutes) + 5;

            _logger.LogInformation(
                "增量更新 {Symbol} {TimeFrame}：从 {LatestTime} 到现在，时间差 {Diff}分钟，预计 {Count} 根 K 线",
                symbol, timeFrame, latestTime, timeDiff.TotalMinutes, requiredCount);

            var serviceCandles = await _oandaService.GetHistoricalDataAsync(symbol, timeFrame, requiredCount);
            var allCandles = serviceCandles;

            _logger.LogInformation(
                "从OANDA获取了 {Count} 根K线，时间范围: {From} 到 {To}",
                allCandles.Count,
                allCandles.FirstOrDefault()?.DateTime,
                allCandles.LastOrDefault()?.DateTime);

            // 保存 >= 最新时间的数据（包括未完成的K线，让UpsertReplace自动更新）
            var newCandles = allCandles
                .Where(c => c.DateTime >= latestTime.Value)
                .ToList();

            _logger.LogInformation(
                "过滤后需要保存 {Total} 根K线，时间范围: {From} 到 {To}",
                newCandles.Count,
                newCandles.FirstOrDefault()?.DateTime,
                newCandles.LastOrDefault()?.DateTime);

            if (newCandles.Any())
            {
                await _repository.SaveBatchAsync(symbol, timeFrame, newCandles);

                var completeCount = newCandles.Count(c => c.IsComplete);
                var incompleteCount = newCandles.Count(c => !c.IsComplete);

                _logger.LogInformation(
                    "增量更新完成：{Symbol} {TimeFrame}，保存 {Total} 根 K 线（完成: {Complete}, 未完成: {Incomplete}）",
                    symbol, timeFrame, newCandles.Count, completeCount, incompleteCount);

                // 执行形态识别并保存预处理数据
                await ProcessAndSavePatternDataAsync(symbol, timeFrame, newCandles);
            }
            else
            {
                _logger.LogInformation(
                    "{Symbol} {TimeFrame} 没有新数据",
                    symbol, timeFrame);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "增量更新 {Symbol} {TimeFrame} 失败",
                symbol, timeFrame);
            throw;
        }
    }

    /// <summary>
    /// 批量增量更新（更新所有配置的品种和周期）
    /// </summary>
    public async Task IncrementalUpdateAllAsync()
    {
        _logger.LogInformation("开始批量增量更新...");

        var tasks = new List<Task>();

        foreach (var symbol in _settings.PreloadSymbols)
        {
            foreach (var timeFrame in _settings.PreloadTimeFrames)
            {
                tasks.Add(IncrementalUpdateWithDelayAsync(symbol, timeFrame));
            }
        }

        await Task.WhenAll(tasks);

        _logger.LogInformation("批量增量更新完成");
    }

    private async Task IncrementalUpdateWithDelayAsync(string symbol, string timeFrame)
    {
        try
        {
            await IncrementalUpdateAsync(symbol, timeFrame);
            // 避免 API 速率限制
            await Task.Delay(500);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "增量更新 {Symbol} {TimeFrame} 失败",
                symbol, timeFrame);
        }
    }

    /// <summary>
    /// 获取初始化数据数量
    /// </summary>
    private int GetInitialDataCount(string timeFrame)
    {
        return timeFrame.ToUpper() switch
        {
            "M1" => 1000,  // 约 16 小时
            "M5" => 2000,  // 约 1 周
            "M15" => 2000, // 约 3 周
            "M30" => 1500, // 约 1 个月
            "H1" => 1000,  // 约 6 周
            "H4" => 500,   // 约 3 个月
            "D1" => 200,   // 约 200 个交易日
            _ => 500
        };
    }

    /// <summary>
    /// 获取时间周期对应的分钟数
    /// </summary>
    private int GetTimeFrameMinutes(string timeFrame)
    {
        return timeFrame.ToUpper() switch
        {
            "M1" => 1,
            "M5" => 5,
            "M15" => 15,
            "M30" => 30,
            "H1" => 60,
            "H4" => 240,
            "D1" => 1440,
            _ => 5
        };
    }

    /// <summary>
    /// 检查数据完整性
    /// </summary>
    public async Task<Dictionary<string, object>> CheckDataIntegrityAsync()
    {
        var report = new Dictionary<string, object>();
        var issues = new List<string>();

        foreach (var symbol in _settings.PreloadSymbols)
        {
            foreach (var timeFrame in _settings.PreloadTimeFrames)
            {
                try
                {
                    var earliestTime = await _repository.GetEarliestTimeAsync(symbol, timeFrame);
                    var latestTime = await _repository.GetLatestTimeAsync(symbol, timeFrame);

                    if (earliestTime == null || latestTime == null)
                    {
                        issues.Add($"{symbol} {timeFrame}: 没有数据");
                        continue;
                    }

                    var expectedCount = CalculateExpectedCount(earliestTime.Value, latestTime.Value, timeFrame);
                    var actualCandles = await _repository.GetRangeAsync(
                        symbol, timeFrame, earliestTime.Value, latestTime.Value);

                    var completeness = (double)actualCandles.Count / expectedCount * 100;

                    report[$"{symbol}_{timeFrame}"] = new
                    {
                        EarliestTime = earliestTime,
                        LatestTime = latestTime,
                        ExpectedCount = expectedCount,
                        ActualCount = actualCandles.Count,
                        Completeness = $"{completeness:F2}%"
                    };

                    if (completeness < 90)
                    {
                        issues.Add($"{symbol} {timeFrame}: 数据完整性 {completeness:F2}%（低于90%）");
                    }
                }
                catch (Exception ex)
                {
                    issues.Add($"{symbol} {timeFrame}: 检查失败 - {ex.Message}");
                }
            }
        }

        report["Issues"] = issues;
        report["TotalIssues"] = issues.Count;

        return report;
    }

    private int CalculateExpectedCount(DateTime start, DateTime end, string timeFrame)
    {
        var minutes = GetTimeFrameMinutes(timeFrame);
        var totalMinutes = (end - start).TotalMinutes;
        return (int)Math.Ceiling(totalMinutes / minutes);
    }

    /// <summary>
    /// 处理并保存形态识别数据
    /// </summary>
    private async Task ProcessAndSavePatternDataAsync(string symbol, string timeFrame, List<Candle> candles)
    {
        try
        {
            if (!candles.Any())
            {
                return;
            }

            _logger.LogInformation(
                "开始为 {Symbol} {TimeFrame} 处理 {Count} 根K线的形态识别...",
                symbol, timeFrame, candles.Count);

            var processedEntities = new List<ProcessedDataEntity>();

            // 需要至少20根K线才能计算EMA20
            if (candles.Count < 20)
            {
                _logger.LogWarning(
                    "{Symbol} {TimeFrame} 数据不足（{Count} < 20），跳过形态识别",
                    symbol, timeFrame, candles.Count);
                return;
            }

            // 按时间排序
            var sortedCandles = candles.OrderBy(c => c.DateTime).ToList();

            // 从第20根开始处理（前19根用于计算EMA）
            for (int i = 20; i < sortedCandles.Count; i++)
            {
                var currentCandle = sortedCandles[i];
                var previousCandles = sortedCandles.Take(i + 1).ToList();

                // 计算EMA20（必须先计算，因为后面要用）
                var ema20 = _patternService.CalculateEMA(previousCandles, 20);

                // 计算技术指标
                var bodyPercent = _indicatorService.CalculateBodyPercent(currentCandle);
                var range = _indicatorService.CalculateRange(currentCandle);
                var bodySizePercent = _indicatorService.CalculateBodySizePercent(currentCandle);
                var upperTailPercent = _indicatorService.CalculateUpperTailPercent(currentCandle);
                var lowerTailPercent = _indicatorService.CalculateLowerTailPercent(currentCandle);
                var distanceToEma = _indicatorService.CalculateDistanceToEMA(currentCandle.Close, ema20, symbol);

                // 形态识别（传入索引和 EMA20）
                var patterns = _patternService.RecognizePatterns(previousCandles, i, ema20, symbol);

                // 创建实体
                var entity = ProcessedDataEntity.Create(
                    symbol: symbol,
                    timeFrame: timeFrame,
                    time: currentCandle.DateTime,
                    bodyPercent: (double)bodyPercent,
                    distanceToEMA: (double)distanceToEma,
                    range: (double)range,
                    bodySizePercent: (double)bodySizePercent,
                    upperTailPercent: (double)upperTailPercent,
                    lowerTailPercent: (double)lowerTailPercent,
                    ema20: (double)ema20,
                    tags: patterns,
                    isSignalBar: patterns.Contains("Signal"),
                    open: currentCandle.Open,
                    high: currentCandle.High,
                    low: currentCandle.Low,
                    close: currentCandle.Close,
                    volume: currentCandle.TickVolume
                );

                processedEntities.Add(entity);
            }

            if (processedEntities.Any())
            {
                await _processedDataRepository.SaveBatchAsync(processedEntities);

                _logger.LogInformation(
                    "成功保存 {Symbol} {TimeFrame} 的 {Count} 条预处理数据",
                    symbol, timeFrame, processedEntities.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "处理 {Symbol} {TimeFrame} 形态识别数据失败",
                symbol, timeFrame);
            // 不抛出异常，避免影响主流程
        }
    }
}
