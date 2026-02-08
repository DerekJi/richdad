using Microsoft.AspNetCore.Mvc;
using Trading.AlertSystem.Data.Models;
using Trading.AlertSystem.Data.Repositories;

namespace Trading.AlertSystem.Web.Controllers;

/// <summary>
/// 告警历史API控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AlertHistoryController : ControllerBase
{
    private readonly IAlertHistoryRepository _repository;
    private readonly ILogger<AlertHistoryController> _logger;

    public AlertHistoryController(
        IAlertHistoryRepository repository,
        ILogger<AlertHistoryController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有告警历史（支持分页和筛选）
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] AlertHistoryType? type = null,
        [FromQuery] string? symbol = null,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null)
    {
        try
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            var (items, totalCount) = await _repository.GetAllAsync(
                pageNumber, pageSize, type, symbol, startTime, endTime);

            return Ok(new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Items = items
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取告警历史失败");
            return StatusCode(500, new { Error = "获取告警历史失败" });
        }
    }

    /// <summary>
    /// 获取最近的告警历史
    /// </summary>
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 100)
    {
        try
        {
            if (count < 1 || count > 500) count = 100;

            var items = await _repository.GetRecentAsync(count);
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取最近告警历史失败");
            return StatusCode(500, new { Error = "获取最近告警历史失败" });
        }
    }

    /// <summary>
    /// 根据ID获取单个告警历史
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null)
            {
                return NotFound(new { Error = "告警历史不存在" });
            }

            return Ok(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取告警历史失败: {Id}", id);
            return StatusCode(500, new { Error = "获取告警历史失败" });
        }
    }

    /// <summary>
    /// 根据品种获取告警历史
    /// </summary>
    [HttpGet("symbol/{symbol}")]
    public async Task<IActionResult> GetBySymbol(string symbol, [FromQuery] int limit = 100)
    {
        try
        {
            if (limit < 1 || limit > 500) limit = 100;

            var items = await _repository.GetBySymbolAsync(symbol, limit);
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取品种告警历史失败: {Symbol}", symbol);
            return StatusCode(500, new { Error = "获取品种告警历史失败" });
        }
    }

    /// <summary>
    /// 根据类型获取告警历史
    /// </summary>
    [HttpGet("type/{type}")]
    public async Task<IActionResult> GetByType(AlertHistoryType type, [FromQuery] int limit = 100)
    {
        try
        {
            if (limit < 1 || limit > 500) limit = 100;

            var items = await _repository.GetByTypeAsync(type, limit);
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取类型告警历史失败: {Type}", type);
            return StatusCode(500, new { Error = "获取类型告警历史失败" });
        }
    }

    /// <summary>
    /// 获取告警统计信息
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] int days = 7)
    {
        try
        {
            var startTime = DateTime.UtcNow.AddDays(-days);
            var (items, totalCount) = await _repository.GetAllAsync(
                1, int.MaxValue, null, null, startTime, null);

            var stats = new
            {
                TotalCount = totalCount,
                PriceAlertCount = items.Count(i => i.Type == AlertHistoryType.PriceAlert),
                EmaCrossCount = items.Count(i => i.Type == AlertHistoryType.EmaCross),
                SymbolStats = items.GroupBy(i => i.Symbol)
                    .Select(g => new { Symbol = g.Key, Count = g.Count() })
                    .OrderByDescending(s => s.Count)
                    .ToList(),
                DailyStats = items.GroupBy(i => i.AlertTime.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .OrderBy(s => s.Date)
                    .ToList()
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取告警统计失败");
            return StatusCode(500, new { Error = "获取告警统计失败" });
        }
    }

    /// <summary>
    /// 删除指定时间之前的旧记录
    /// </summary>
    [HttpDelete("cleanup")]
    public async Task<IActionResult> CleanupOldRecords([FromQuery] int daysToKeep = 30)
    {
        try
        {
            if (daysToKeep < 1) daysToKeep = 30;

            var beforeDate = DateTime.UtcNow.AddDays(-daysToKeep);
            var deletedCount = await _repository.DeleteOldRecordsAsync(beforeDate);

            return Ok(new
            {
                DeletedCount = deletedCount,
                BeforeDate = beforeDate,
                Message = $"成功删除 {deletedCount} 条旧记录"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理旧告警历史失败");
            return StatusCode(500, new { Error = "清理旧告警历史失败" });
        }
    }
}
