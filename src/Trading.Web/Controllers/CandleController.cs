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
    /// <returns>统计数据</returns>
    /// <remarks>
    /// 示例: GET /api/marketdata/stats
    /// </remarks>
    [HttpGet("stats")]
    [ProducesResponseType(200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> GetStats()
    {
        try
        {
            var stats = await _repository.GetStatisticsAsync();
            return Ok(stats);
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
    /// <param name="symbols">品种列表（可选，默认使用配置）</param>
    /// <param name="timeFrames">时间周期列表（可选，默认使用配置）</param>
    /// <returns>初始化结果</returns>
    /// <remarks>
    /// 示例: POST /api/marketdata/initialize
    /// 或带参数: POST /api/marketdata/initialize?symbols=XAUUSD,XAGUSD&amp;timeFrames=M5,H1
    /// </remarks>
    [HttpPost("initialize")]
    [ProducesResponseType(200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> InitializeData(
        [FromQuery] string? symbols = null,
        [FromQuery] string? timeFrames = null)
    {
        try
        {
            var symbolList = symbols?.Split(',').ToList();
            var timeFrameList = timeFrames?.Split(',').ToList();

            _logger.LogInformation("开始初始化历史数据...");

            await _initService.InitializeHistoricalDataAsync(symbolList, timeFrameList);

            return Ok(new
            {
                message = "历史数据初始化完成",
                symbols = symbolList,
                timeFrames = timeFrameList
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
