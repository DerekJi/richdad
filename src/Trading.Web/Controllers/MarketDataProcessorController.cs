using Microsoft.AspNetCore.Mvc;
using Trading.Services.Services;

namespace Trading.Web.Controllers;

/// <summary>
/// 市场数据处理 API - 测试 Phase 1 实现
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MarketDataProcessorController : ControllerBase
{
    private readonly MarketDataProcessor _processor;
    private readonly ILogger<MarketDataProcessorController> _logger;

    public MarketDataProcessorController(
        MarketDataProcessor processor,
        ILogger<MarketDataProcessorController> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    /// <summary>
    /// 测试完整的数据处理管道
    /// GET /api/marketdataprocessor/test?symbol=XAUUSD&timeFrame=M5&count=80
    /// </summary>
    [HttpGet("test")]
    public async Task<IActionResult> TestProcessing(
        [FromQuery] string symbol = "XAUUSD",
        [FromQuery] string timeFrame = "M5",
        [FromQuery] int count = 80)
    {
        try
        {
            _logger.LogInformation("测试市场数据处理: {Symbol} {TimeFrame}, Count={Count}",
                symbol, timeFrame, count);

            var result = await _processor.ProcessMarketDataAsync(symbol, timeFrame, count, useCache: false);

            return Ok(new
            {
                symbol = result.Symbol,
                timeFrame = result.TimeFrame,
                candleCount = result.CandleCount,
                startTime = result.StartTime,
                endTime = result.EndTime,
                currentPrice = result.CurrentPrice,
                currentEMA20 = result.CurrentEMA20,
                contextTableLength = result.ContextTable.Length,
                focusTableLength = result.FocusTable.Length,
                patternSummaryLength = result.PatternSummary.Length,
                patternCount = result.PatternsByIndex.Count(p => p.Value.Any()),
                // 返回前 1000 字符的表格（避免响应过大）
                contextTablePreview = result.ContextTable.Length > 1000
                    ? result.ContextTable.Substring(0, 1000) + "..."
                    : result.ContextTable,
                focusTable = result.FocusTable,
                patternSummary = result.PatternSummary
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试处理失败");
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// 获取完整的 Markdown 表格（用于 AI Prompt）
    /// GET /api/marketdataprocessor/markdown?symbol=XAUUSD&timeFrame=M5&count=80
    /// </summary>
    [HttpGet("markdown")]
    public async Task<IActionResult> GetMarkdownData(
        [FromQuery] string symbol = "XAUUSD",
        [FromQuery] string timeFrame = "M5",
        [FromQuery] int count = 80,
        [FromQuery] bool fullTable = false)
    {
        try
        {
            var result = await _processor.ProcessMarketDataAsync(symbol, timeFrame, count);

            var response = new
            {
                symbol = result.Symbol,
                timeFrame = result.TimeFrame,
                candleCount = result.CandleCount,
                table = fullTable ? result.ContextTable : result.FocusTable,
                patternSummary = result.PatternSummary
            };

            // 返回纯文本格式（便于直接查看）
            var markdown = $@"# {result.Symbol} {result.TimeFrame} - Market Analysis Data

## Current State
- Candle Count: {result.CandleCount}
- Time Range: {result.StartTime:yyyy-MM-dd HH:mm} to {result.EndTime:yyyy-MM-dd HH:mm}
- Current Price: {result.CurrentPrice:F2}
- Current EMA20: {result.CurrentEMA20:F2}

{(fullTable ? result.ContextTable : result.FocusTable)}

{result.PatternSummary}
";

            return Content(markdown, "text/markdown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 Markdown 数据失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 测试缓存性能
    /// GET /api/marketdataprocessor/benchmark?symbol=XAUUSD&timeFrame=M5&count=80&iterations=10
    /// </summary>
    [HttpGet("benchmark")]
    public async Task<IActionResult> Benchmark(
        [FromQuery] string symbol = "XAUUSD",
        [FromQuery] string timeFrame = "M5",
        [FromQuery] int count = 80,
        [FromQuery] int iterations = 10)
    {
        try
        {
            var results = new List<long>();

            // 热身
            await _processor.ProcessMarketDataAsync(symbol, timeFrame, count);

            // 基准测试（使用缓存）
            for (int i = 0; i < iterations; i++)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                await _processor.ProcessMarketDataAsync(symbol, timeFrame, count, useCache: true);
                sw.Stop();
                results.Add(sw.ElapsedMilliseconds);
            }

            var withCacheAvg = results.Average();
            results.Clear();

            // 基准测试（不使用缓存）
            for (int i = 0; i < iterations; i++)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                await _processor.ProcessMarketDataAsync(symbol, timeFrame, count, useCache: false);
                sw.Stop();
                results.Add(sw.ElapsedMilliseconds);
            }

            var withoutCacheAvg = results.Average();

            return Ok(new
            {
                symbol,
                timeFrame,
                count,
                iterations,
                withCacheAvgMs = withCacheAvg,
                withoutCacheAvgMs = withoutCacheAvg,
                speedup = withoutCacheAvg / withCacheAvg
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "基准测试失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
