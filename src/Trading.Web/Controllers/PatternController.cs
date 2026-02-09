using Microsoft.AspNetCore.Mvc;
using Trading.Infrastructure.Repositories;

namespace Trading.Web.Controllers;

/// <summary>
/// 形态识别 API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PatternController : ControllerBase
{
    private readonly IProcessedDataRepository _repository;
    private readonly ILogger<PatternController> _logger;

    public PatternController(
        IProcessedDataRepository repository,
        ILogger<PatternController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// 获取预处理数据
    /// GET /api/pattern/processed?symbol=XAUUSD&timeFrame=M5&startTime=2024-01-01&endTime=2024-01-31
    /// </summary>
    [HttpGet("processed")]
    public async Task<IActionResult> GetProcessedData(
        [FromQuery] string symbol,
        [FromQuery] string timeFrame,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null,
        [FromQuery] int? count = 100)
    {
        try
        {
            if (string.IsNullOrEmpty(symbol) || string.IsNullOrEmpty(timeFrame))
            {
                return BadRequest(new { error = "Symbol and TimeFrame are required" });
            }

            List<Dictionary<string, object>> results;

            if (startTime.HasValue && endTime.HasValue)
            {
                // 按时间范围查询
                var entities = await _repository.GetRangeAsync(
                    symbol,
                    timeFrame,
                    startTime.Value,
                    endTime.Value);

                results = entities.Select(e => e.ToDictionary()).ToList();
            }
            else
            {
                // 获取最新的 count 条数据
                var maxCount = count ?? 100;
                var endDateTime = DateTime.UtcNow;
                var startDateTime = endDateTime.AddDays(-30); // 默认查询最近30天

                var entities = await _repository.GetRangeAsync(
                    symbol,
                    timeFrame,
                    startDateTime,
                    endDateTime);

                results = entities
                    .OrderByDescending(e => e.Time)
                    .Take(maxCount)
                    .OrderBy(e => e.Time)
                    .Select(e => e.ToDictionary())
                    .ToList();
            }

            return Ok(new
            {
                symbol,
                timeFrame,
                count = results.Count,
                data = results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取预处理数据失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取单条预处理数据
    /// GET /api/pattern/processed/XAUUSD/M5/2024-01-01T10:00:00Z
    /// </summary>
    [HttpGet("processed/{symbol}/{timeFrame}/{time}")]
    public async Task<IActionResult> GetProcessedDataByTime(
        string symbol,
        string timeFrame,
        DateTime time)
    {
        try
        {
            var entity = await _repository.GetByTimeAsync(symbol, timeFrame, time);

            if (entity == null)
            {
                return NotFound(new { error = "Data not found" });
            }

            return Ok(entity.ToDictionary());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取预处理数据失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取数据统计
    /// GET /api/pattern/stats?symbol=XAUUSD&timeFrame=M5
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(
        [FromQuery] string symbol,
        [FromQuery] string timeFrame)
    {
        try
        {
            if (string.IsNullOrEmpty(symbol) || string.IsNullOrEmpty(timeFrame))
            {
                return BadRequest(new { error = "Symbol and TimeFrame are required" });
            }

            var count = await _repository.GetCountAsync(symbol, timeFrame);

            return Ok(new
            {
                symbol,
                timeFrame,
                totalRecords = count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取统计信息失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 生成 Markdown 上下文表格（用于 AI 分析）
    /// GET /api/pattern/markdown?symbol=XAUUSD&timeFrame=M5&count=10
    /// </summary>
    [HttpGet("markdown")]
    public async Task<IActionResult> GetMarkdownContext(
        [FromQuery] string symbol,
        [FromQuery] string timeFrame,
        [FromQuery] int count = 10)
    {
        try
        {
            if (string.IsNullOrEmpty(symbol) || string.IsNullOrEmpty(timeFrame))
            {
                return BadRequest(new { error = "Symbol and TimeFrame are required" });
            }

            // 获取最新数据
            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddDays(-7); // 最近7天

            var entities = await _repository.GetRangeAsync(symbol, timeFrame, startTime, endTime);
            var recentData = entities
                .OrderByDescending(e => e.Time)
                .Take(count)
                .OrderBy(e => e.Time)
                .ToList();

            if (!recentData.Any())
            {
                return NotFound(new { error = "No data found" });
            }

            // 生成 Markdown 表格
            var markdown = GenerateMarkdownTable(recentData, symbol, timeFrame);

            return Ok(new
            {
                symbol,
                timeFrame,
                count = recentData.Count,
                markdown
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成 Markdown 失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private string GenerateMarkdownTable(List<Trading.Infrastructure.Models.ProcessedDataEntity> data, string symbol, string timeFrame)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"## {symbol} {timeFrame} - 形态识别数据");
        sb.AppendLine();
        sb.AppendLine("| Time | Close | Body% | EMA20 | Distance | Tags | Signal |");
        sb.AppendLine("|------|-------|-------|-------|----------|------|--------|");

        foreach (var item in data)
        {
            var tags = string.Join(", ", item.GetTags());
            var signalMark = item.IsSignalBar ? "✓" : "";

            sb.AppendLine($"| {item.Time:MM-dd HH:mm} | {item.Close:F2} | {item.BodyPercent:P0} | {item.EMA20:F2} | {item.DistanceToEMA20:F1} | {tags} | {signalMark} |");
        }

        return sb.ToString();
    }
}
