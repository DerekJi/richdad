using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Trading.Infrastructure.Repositories;
using Trading.Models;
using Trading.Services.Services;

namespace Trading.Web.Controllers;

/// <summary>
/// K线数据控制器 - 提供缓存数据的查询、刷新和统计功能
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CandleController : ControllerBase
{
    private readonly CandleCacheService _cacheService;
    private readonly ICandleRepository _repository;
    private readonly CandleInitializationService _initService;
    private readonly ILogger<CandleController> _logger;

    public CandleController(
        CandleCacheService cacheService,
        ICandleRepository repository,
        CandleInitializationService initService,
        ILogger<CandleController> logger)
    {
        _cacheService = cacheService;
        _repository = repository;
        _initService = initService;
        _logger = logger;
    }

    /// <summary>
    /// 获取 K 线数据（智能缓存）
    /// </summary>
    /// <param name="symbol">品种代码（如 XAUUSD）</param>
    /// <param name="timeFrame">时间周期（如 M5, H1）</param>
    /// <param name="count">K线数量（默认100）</param>
    /// <param name="endTime">结束时间（默认为当前时间）</param>
    /// <returns>K线列表</returns>
    /// <remarks>
    /// 示例: GET /api/marketdata/candles?symbol=XAUUSD&amp;timeFrame=M5&amp;count=200
    /// </remarks>
    [HttpGet("candles")]
    [ProducesResponseType(typeof(List<Candle>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<Candle>>> GetCandles(
        [Required] string symbol,
        [Required] string timeFrame,
        int count = 100,
        DateTime? endTime = null)
    {
        try
        {
            if (count <= 0 || count > 5000)
            {
                return BadRequest(new { error = "count 必须在 1-5000 之间" });
            }

            var candles = await _cacheService.GetCandlesAsync(
                symbol, timeFrame, count, endTime);

            return Ok(candles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "获取 K 线数据失败 ({Symbol} {TimeFrame})",
                symbol, timeFrame);
            return StatusCode(500, new { error = "获取数据失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取最新数据时间
    /// </summary>
    /// <param name="symbol">品种代码</param>
    /// <param name="timeFrame">时间周期</param>
    /// <returns>最新时间信息</returns>
    /// <remarks>
    /// 示例: GET /api/marketdata/latest?symbol=XAUUSD&amp;timeFrame=M5
    /// </remarks>
    [HttpGet("latest")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult> GetLatestTime(
        [Required] string symbol,
        [Required] string timeFrame)
    {
        try
        {
            var latestTime = await _repository.GetLatestTimeAsync(symbol, timeFrame);
            var earliestTime = await _repository.GetEarliestTimeAsync(symbol, timeFrame);

            return Ok(new
            {
                symbol,
                timeFrame,
                latestTime,
                earliestTime,
                hasData = latestTime != null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "获取最新时间失败 ({Symbol} {TimeFrame})",
                symbol, timeFrame);
            return StatusCode(500, new { error = "查询失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 手动刷新缓存
    /// </summary>
    /// <param name="symbol">品种代码</param>
    /// <param name="timeFrame">时间周期</param>
    /// <param name="startTime">开始时间（默认为7天前）</param>
    /// <param name="endTime">结束时间（默认为当前时间）</param>
    /// <returns>刷新结果</returns>
    /// <remarks>
    /// 示例: POST /api/marketdata/refresh?symbol=XAUUSD&amp;timeFrame=M5
    /// </remarks>
    [HttpPost("refresh")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> RefreshCache(
        [Required] string symbol,
        [Required] string timeFrame,
        DateTime? startTime = null,
        DateTime? endTime = null)
    {
        try
        {
            await _cacheService.RefreshCacheAsync(
                symbol, timeFrame, startTime, endTime);

            var latestTime = await _repository.GetLatestTimeAsync(symbol, timeFrame);

            return Ok(new
            {
                message = "缓存已刷新",
                symbol,
                timeFrame,
                latestTime
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "刷新缓存失败 ({Symbol} {TimeFrame})",
                symbol, timeFrame);
            return StatusCode(500, new { error = "刷新失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    /// <param name="symbol">品种代码（可选）</param>
    /// <param name="timeFrame">时间周期（可选）</param>
    /// <returns>统计数据</returns>
    /// <remarks>
    /// 示例: GET /api/candle/stats 或 GET /api/candle/stats?symbol=XAUUSD&amp;timeFrame=M5
    /// </remarks>
    [HttpGet("stats")]
    [ProducesResponseType(200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> GetStats(string? symbol = null, string? timeFrame = null)
    {
        try
        {
            if (!string.IsNullOrEmpty(symbol) && !string.IsNullOrEmpty(timeFrame))
            {
                // 获取特定品种和时间周期的详细统计
                var count = await _repository.GetCountAsync(symbol, timeFrame);
                var latestTime = await _repository.GetLatestTimeAsync(symbol, timeFrame);
                var earliestTime = await _repository.GetEarliestTimeAsync(symbol, timeFrame);

                // 获取最近的几条记录作为样本
                var sampleData = await _repository.GetRangeAsync(
                    symbol,
                    timeFrame,
                    latestTime?.AddHours(-1) ?? DateTime.UtcNow.AddHours(-1),
                    latestTime ?? DateTime.UtcNow);

                return Ok(new
                {
                    symbol,
                    timeFrame,
                    totalRecords = count,
                    earliestTime,
                    latestTime,
                    dataSpan = latestTime.HasValue && earliestTime.HasValue
                        ? (latestTime.Value - earliestTime.Value).TotalDays.ToString("F2") + " 天"
                        : "无数据",
                    sampleRecords = sampleData.Take(5).Select(c => new
                    {
                        time = c.DateTime,
                        open = c.Open,
                        high = c.High,
                        low = c.Low,
                        close = c.Close,
                        volume = c.TickVolume
                    })
                });
            }
            else
            {
                // 获取所有数据的统计
                var stats = await _repository.GetStatisticsAsync();
                return Ok(stats);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取统计信息失败");
            return StatusCode(500, new { error = "获取统计失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 初始化历史数据
    /// </summary>
    /// <param name="request">初始化请求（支持JSON body）</param>
    /// <param name="symbols">品种列表，逗号分隔（URL参数）</param>
    /// <param name="timeFrames">时间周期列表，逗号分隔（URL参数）</param>
    /// <returns>初始化结果</returns>
    /// <remarks>
    /// 方式A - URL参数: POST /api/candle/initialize?symbols=XAUUSD&amp;timeFrames=M5
    /// 方式B - JSON body: POST /api/candle/initialize
    /// Body: {"symbol":"XAUUSD","timeFrame":"M5","days":30}
    /// 或批量: {"symbols":["XAUUSD","XAGUSD"],"timeFrames":["M5","H1"]}
    /// </remarks>
    [HttpPost("initialize")]
    [ProducesResponseType(200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> InitializeData(
        [FromBody] InitializeDataRequest? request = null,
        [FromQuery] string? symbols = null,
        [FromQuery] string? timeFrames = null)
    {
        try
        {
            List<string>? symbolList = null;
            List<string>? timeFrameList = null;

            // 优先使用 JSON body 参数
            if (request != null)
            {
                // 支持单个品种/周期
                if (!string.IsNullOrEmpty(request.Symbol))
                {
                    symbolList = new List<string> { request.Symbol };
                }
                if (!string.IsNullOrEmpty(request.TimeFrame))
                {
                    timeFrameList = new List<string> { request.TimeFrame };
                }

                // 支持批量品种/周期（覆盖单个）
                if (request.Symbols?.Any() == true)
                {
                    symbolList = request.Symbols;
                }
                if (request.TimeFrames?.Any() == true)
                {
                    timeFrameList = request.TimeFrames;
                }
            }
            // 其次使用 URL 参数
            else if (!string.IsNullOrEmpty(symbols) || !string.IsNullOrEmpty(timeFrames))
            {
                symbolList = symbols?.Split(',').ToList();
                timeFrameList = timeFrames?.Split(',').ToList();
            }

            // 提取count参数（优先）或days参数
            int? count = request?.Count;

            // 如果指定了days但没有count，根据days计算count（仅用于D1）
            if (count == null && request?.Days > 0)
            {
                // 对于D1，days直接等于count
                // 对于其他周期，给出提示
                count = request.Days;
                _logger.LogInformation("使用days参数: {Days}，建议使用count参数更精确", request.Days);
            }

            _logger.LogInformation("开始初始化历史数据... Symbols: {Symbols}, TimeFrames: {TimeFrames}, Count: {Count}",
                symbolList != null ? string.Join(",", symbolList) : "使用默认配置",
                timeFrameList != null ? string.Join(",", timeFrameList) : "使用默认配置",
                count?.ToString() ?? "使用默认值");

            await _initService.InitializeHistoricalDataAsync(symbolList, timeFrameList, count);

            return Ok(new
            {
                message = "历史数据初始化完成",
                symbols = symbolList ?? new List<string>(),
                timeFrames = timeFrameList ?? new List<string>(),
                count = count ?? 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化历史数据失败");
            return StatusCode(500, new { error = "初始化失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 增量更新数据
    /// </summary>
    /// <param name="symbol">品种代码（可选，为空则更新所有）</param>
    /// <param name="timeFrame">时间周期（可选，为空则更新所有）</param>
    /// <returns>更新结果</returns>
    /// <remarks>
    /// 示例: POST /api/marketdata/update
    /// 或更新特定品种: POST /api/marketdata/update?symbol=XAUUSD&amp;timeFrame=M5
    /// </remarks>
    [HttpPost("update")]
    [ProducesResponseType(200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> IncrementalUpdate(
        string? symbol = null,
        string? timeFrame = null)
    {
        try
        {
            if (string.IsNullOrEmpty(symbol) || string.IsNullOrEmpty(timeFrame))
            {
                // 更新所有配置的品种和周期
                _logger.LogInformation("开始批量增量更新...");
                await _initService.IncrementalUpdateAllAsync();

                return Ok(new
                {
                    message = "批量增量更新完成"
                });
            }
            else
            {
                // 更新特定品种和周期
                _logger.LogInformation("增量更新 {Symbol} {TimeFrame}", symbol, timeFrame);
                await _initService.IncrementalUpdateAsync(symbol, timeFrame);

                var latestTime = await _repository.GetLatestTimeAsync(symbol, timeFrame);

                return Ok(new
                {
                    message = "增量更新完成",
                    symbol,
                    timeFrame,
                    latestTime
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "增量更新失败");
            return StatusCode(500, new { error = "更新失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 检查数据完整性
    /// </summary>
    /// <returns>完整性报告</returns>
    /// <remarks>
    /// 示例: GET /api/marketdata/integrity
    /// </remarks>
    [HttpGet("integrity")]
    [ProducesResponseType(200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> CheckIntegrity()
    {
        try
        {
            _logger.LogInformation("开始检查数据完整性...");
            var report = await _initService.CheckDataIntegrityAsync();

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查数据完整性失败");
            return StatusCode(500, new { error = "检查失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 删除指定时间范围的数据
    /// </summary>
    /// <param name="symbol">品种代码</param>
    /// <param name="timeFrame">时间周期</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns>删除结果</returns>
    /// <remarks>
    /// 示例: DELETE /api/marketdata?symbol=XAUUSD&amp;timeFrame=M5&amp;startTime=2026-01-01&amp;endTime=2026-01-31
    /// </remarks>
    [HttpDelete]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> DeleteRange(
        [Required] string symbol,
        [Required] string timeFrame,
        [Required] DateTime startTime,
        [Required] DateTime endTime)
    {
        try
        {
            if (startTime >= endTime)
            {
                return BadRequest(new { error = "startTime 必须小于 endTime" });
            }

            await _repository.DeleteRangeAsync(symbol, timeFrame, startTime, endTime);

            return Ok(new
            {
                message = "数据已删除",
                symbol,
                timeFrame,
                startTime,
                endTime
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "删除数据失败 ({Symbol} {TimeFrame})",
                symbol, timeFrame);
            return StatusCode(500, new { error = "删除失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 预加载数据
    /// </summary>
    /// <returns>预加载结果</returns>
    /// <remarks>
    /// 示例: POST /api/marketdata/preload
    /// </remarks>
    [HttpPost("preload")]
    [ProducesResponseType(200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> PreloadData()
    {
        try
        {
            _logger.LogInformation("开始预加载数据...");
            await _cacheService.PreloadDataAsync();

            return Ok(new
            {
                message = "数据预加载完成"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "预加载数据失败");
            return StatusCode(500, new { error = "预加载失败", message = ex.Message });
        }
    }
}
