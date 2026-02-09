using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Trading.Infrastructure.Configuration;
using Trading.Infrastructure.Repositories;
using Trading.Infrastructure.Services;
using Candle = Trading.Models.Candle; // 明确使用 Trading.Models.Candle

namespace Trading.Services.Services;

/// <summary>
/// 市场数据缓存服务 - 智能缓存，优先从数据库查询，仅补充缺失数据
/// </summary>
public class CandleCacheService
{
    private readonly IOandaService _oandaService;
    private readonly ICandleRepository _repository;
    private readonly ILogger<CandleCacheService> _logger;
    private readonly CandleCacheSettings _settings;

    public CandleCacheService(
        IOandaService oandaService,
        ICandleRepository repository,
        ILogger<CandleCacheService> logger,
        IOptions<CandleCacheSettings> settings)
    {
        _oandaService = oandaService;
        _repository = repository;
        _logger = logger;
        _settings = settings.Value;
    }

    /// <summary>
    /// 智能获取 K 线数据：优先从数据库查询，仅补充缺失部分
    /// </summary>
    /// <param name="symbol">品种代码（如 XAUUSD）</param>
    /// <param name="timeFrame">时间周期（如 M5）</param>
    /// <param name="count">需要的 K 线数量</param>
    /// <param name="endTime">结束时间（默认为当前时间）</param>
    /// <returns>K 线列表</returns>
    public async Task<List<Candle>> GetCandlesAsync(
        string symbol,
        string timeFrame,
        int count,
        DateTime? endTime = null)
    {
        if (!_settings.EnableSmartCache)
        {
            // 如果未启用缓存，直接从 OANDA 获取并转换
            _logger.LogDebug("缓存未启用，直接从 OANDA 获取数据");
            var serviceCandles = await _oandaService.GetHistoricalDataAsync(symbol, timeFrame, count);
            return serviceCandles;
        }

        endTime ??= DateTime.UtcNow;
        var startTime = CalculateStartTime(endTime.Value, timeFrame, count);

        try
        {
            // 1. 从数据库查询已有数据
            var cachedData = await _repository.GetRangeAsync(
                symbol, timeFrame, startTime, endTime.Value);

            _logger.LogInformation(
                "从缓存获取 {CachedCount}/{RequestCount} 根 K 线 ({Symbol} {TimeFrame})",
                cachedData.Count, count, symbol, timeFrame);

            // 2. 检测缺失的时间段
            var missingRanges = DetectMissingRanges(
                startTime, endTime.Value, timeFrame, cachedData);

            if (missingRanges.Any())
            {
                _logger.LogInformation(
                    "检测到 {Count} 个缺失时间段，从 OANDA 补充数据",
                    missingRanges.Count);

                // 3. 从 OANDA API 获取缺失数据
                foreach (var range in missingRanges)
                {
                    var requiredCount = CalculateRequiredCount(range.Start, range.End, timeFrame);
                    var freshData = await _oandaService.GetHistoricalDataAsync(
                        symbol, timeFrame, requiredCount);

                    if (freshData.Any())
                    {
                        // 转换为 Models.Candle
                        var modelCandles = freshData;

                        // 过滤出指定时间范围内的数据
                        var filteredData = modelCandles
                            .Where(c => c.DateTime >= range.Start && c.DateTime <= range.End)
                            .ToList();

                        // 4. 保存到数据库
                        if (filteredData.Any())
                        {
                            await _repository.SaveBatchAsync(symbol, timeFrame, filteredData);
                            cachedData.AddRange(filteredData);
                        }
                    }
                }
            }

            // 5. 按时间排序并返回
            return cachedData
                .OrderBy(c => c.DateTime)
                .TakeLast(count)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "智能缓存获取数据失败 ({Symbol} {TimeFrame})，回退到直接 API 调用",
                symbol, timeFrame);

            // 失败时回退到直接从 OANDA 获取并转换
            var serviceCandles = await _oandaService.GetHistoricalDataAsync(symbol, timeFrame, count);
            return serviceCandles;
        }
    }

    /// <summary>
    /// 刷新指定品种和时间周期的缓存
    /// </summary>
    public async Task RefreshCacheAsync(
        string symbol,
        string timeFrame,
        DateTime? startTime = null,
        DateTime? endTime = null)
    {
        startTime ??= DateTime.UtcNow.AddDays(-7);
        endTime ??= DateTime.UtcNow;

        try
        {
            var count = CalculateRequiredCount(startTime.Value, endTime.Value, timeFrame);
            var serviceCandles = await _oandaService.GetHistoricalDataAsync(symbol, timeFrame, count);
            var candles = serviceCandles;

            if (candles.Any())
            {
                await _repository.SaveBatchAsync(symbol, timeFrame, candles);
                _logger.LogInformation(
                    "缓存刷新成功：{Symbol} {TimeFrame}，共 {Count} 根 K 线",
                    symbol, timeFrame, candles.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新缓存失败 ({Symbol} {TimeFrame})", symbol, timeFrame);
            throw;
        }
    }

    /// <summary>
    /// 预加载配置中指定的品种数据
    /// </summary>
    public async Task PreloadDataAsync()
    {
        _logger.LogInformation("开始预加载市场数据...");

        var tasks = new List<Task>();

        foreach (var symbol in _settings.PreloadSymbols)
        {
            foreach (var timeFrame in _settings.PreloadTimeFrames)
            {
                tasks.Add(PreloadSymbolDataAsync(symbol, timeFrame));
            }
        }

        await Task.WhenAll(tasks);

        _logger.LogInformation("市场数据预加载完成");
    }

    private async Task PreloadSymbolDataAsync(string symbol, string timeFrame)
    {
        try
        {
            // 检查是否已有数据
            var latestTime = await _repository.GetLatestTimeAsync(symbol, timeFrame);

            if (latestTime == null)
            {
                // 没有数据，获取历史数据
                var serviceCandles = await _oandaService.GetHistoricalDataAsync(
                    symbol, timeFrame, _settings.PreloadCandleCount);
                var candles = serviceCandles;

                if (candles.Any())
                {
                    await _repository.SaveBatchAsync(symbol, timeFrame, candles);
                    _logger.LogInformation(
                        "预加载 {Symbol} {TimeFrame}：{Count} 根 K 线",
                        symbol, timeFrame, candles.Count);
                }
            }
            else
            {
                // 已有数据，仅补充最新数据
                var now = DateTime.UtcNow;
                if ((now - latestTime.Value).TotalMinutes > GetTimeFrameMinutes(timeFrame))
                {
                    await RefreshCacheAsync(symbol, timeFrame, latestTime.Value, now);
                }
            }

            // 避免 API 速率限制
            await Task.Delay(500);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "预加载 {Symbol} {TimeFrame} 失败", symbol, timeFrame);
        }
    }

    /// <summary>
    /// 计算起始时间
    /// </summary>
    private DateTime CalculateStartTime(DateTime endTime, string timeFrame, int count)
    {
        var minutes = GetTimeFrameMinutes(timeFrame);
        return endTime.AddMinutes(-count * minutes);
    }

    /// <summary>
    /// 计算需要的 K 线数量
    /// </summary>
    private int CalculateRequiredCount(DateTime startTime, DateTime endTime, string timeFrame)
    {
        var minutes = GetTimeFrameMinutes(timeFrame);
        var totalMinutes = (endTime - startTime).TotalMinutes;
        return (int)Math.Ceiling(totalMinutes / minutes) + 10; // 额外增加10根以防万一
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
    /// 检测缺失的时间段
    /// </summary>
    private List<TimeRange> DetectMissingRanges(
        DateTime start,
        DateTime end,
        string timeFrame,
        List<Candle> existingData)
    {
        if (!existingData.Any())
        {
            // 没有任何数据，整个区间都缺失
            return new List<TimeRange> { new(start, end) };
        }

        var ranges = new List<TimeRange>();
        var minutes = GetTimeFrameMinutes(timeFrame);
        var expectedTimes = new HashSet<DateTime>();

        // 生成预期的所有时间点
        for (var time = start; time <= end; time = time.AddMinutes(minutes))
        {
            // 标准化时间（移除秒和毫秒）
            expectedTimes.Add(new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0));
        }

        var existingTimes = existingData
            .Select(c => new DateTime(c.DateTime.Year, c.DateTime.Month, c.DateTime.Day, c.DateTime.Hour, c.DateTime.Minute, 0))
            .ToHashSet();

        var missingTimes = expectedTimes
            .Where(t => !existingTimes.Contains(t))
            .OrderBy(t => t)
            .ToList();

        if (!missingTimes.Any())
        {
            return ranges;
        }

        // 将连续的缺失时间合并为时间段
        DateTime? rangeStart = null;
        DateTime? rangeEnd = null;

        foreach (var time in missingTimes)
        {
            if (rangeStart == null)
            {
                rangeStart = time;
                rangeEnd = time;
            }
            else if (rangeEnd != null && (time - rangeEnd.Value).TotalMinutes <= minutes * 2)
            {
                // 连续的时间点
                rangeEnd = time;
            }
            else
            {
                // 不连续，保存当前范围
                if (rangeStart.HasValue && rangeEnd.HasValue)
                {
                    ranges.Add(new TimeRange(rangeStart.Value, rangeEnd.Value));
                }
                rangeStart = time;
                rangeEnd = time;
            }
        }

        // 保存最后一个范围
        if (rangeStart != null && rangeEnd != null)
        {
            ranges.Add(new TimeRange(rangeStart.Value, rangeEnd.Value));
        }

        return ranges;
    }

    /// <summary>
    /// 时间范围记录
    /// </summary>
    private record TimeRange(DateTime Start, DateTime End);
}
