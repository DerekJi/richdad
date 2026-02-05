using Microsoft.AspNetCore.Mvc;
using Trading.AlertSystem.Data.Models;
using Trading.AlertSystem.Data.Repositories;
using Trading.AlertSystem.Service.Services;

namespace Trading.AlertSystem.Web.Controllers;

/// <summary>
/// EMA监控管理API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EmaMonitorController : ControllerBase
{
    private readonly IEmaMonitorRepository _repository;
    private readonly IEmaMonitoringService _monitoringService;
    private readonly ILogger<EmaMonitorController> _logger;

    public EmaMonitorController(
        IEmaMonitorRepository repository,
        IEmaMonitoringService monitoringService,
        ILogger<EmaMonitorController> logger)
    {
        _repository = repository;
        _monitoringService = monitoringService;
        _logger = logger;
    }

    /// <summary>
    /// 获取当前EMA配置
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<EmaMonitoringConfig>> GetConfig()
    {
        try
        {
            var config = await _repository.GetConfigAsync();
            if (config == null)
            {
                return NotFound(new { message = "EMA配置不存在" });
            }

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取EMA配置失败");
            return StatusCode(500, new { message = "获取配置失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 更新EMA配置
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<EmaMonitoringConfig>> UpdateConfig([FromBody] EmaMonitoringConfig config)
    {
        try
        {
            // 验证配置
            if (config.Symbols == null || config.Symbols.Count == 0)
            {
                return BadRequest(new { message = "至少需要配置一个交易品种" });
            }

            if (config.TimeFrames == null || config.TimeFrames.Count == 0)
            {
                return BadRequest(new { message = "至少需要配置一个时间周期" });
            }

            if (config.EmaPeriods == null || config.EmaPeriods.Count == 0)
            {
                return BadRequest(new { message = "至少需要配置一个EMA周期" });
            }

            if (config.HistoryMultiplier < 1 || config.HistoryMultiplier > 10)
            {
                return BadRequest(new { message = "历史数据倍数必须在1-10之间" });
            }

            // 验证时间周期格式
            var validTimeFrames = new[] { "M1", "M5", "M15", "M30", "H1", "H4", "D1" };
            foreach (var tf in config.TimeFrames)
            {
                if (!validTimeFrames.Contains(tf))
                {
                    return BadRequest(new { message = $"无效的时间周期: {tf}" });
                }
            }

            // 验证EMA周期
            foreach (var period in config.EmaPeriods)
            {
                if (period < 1 || period > 500)
                {
                    return BadRequest(new { message = $"无效的EMA周期: {period}（必须在1-500之间）" });
                }
            }

            // 保存配置
            config.UpdatedBy = User?.Identity?.Name ?? "Anonymous";
            var savedConfig = await _repository.SaveConfigAsync(config);

            // 重新加载服务配置
            await _monitoringService.ReloadConfigAsync();

            _logger.LogInformation("EMA配置已更新并重新加载：Enabled={Enabled}", config.Enabled);

            return Ok(savedConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新EMA配置失败");
            return StatusCode(500, new { message = "更新配置失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取EMA监测状态
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult> GetStatus()
    {
        try
        {
            var config = await _repository.GetConfigAsync();
            var states = await _monitoringService.GetStatesAsync();

            return Ok(new
            {
                config = config,
                enabled = _monitoringService.IsEnabled(),
                stateCount = states.Count(),
                states = states.Take(10) // 仅返回前10个状态
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取EMA状态失败");
            return StatusCode(500, new { message = "获取状态失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 重新加载配置（不修改数据库，仅重新读取）
    /// </summary>
    [HttpPost("reload")]
    public async Task<ActionResult> ReloadConfig()
    {
        try
        {
            await _monitoringService.ReloadConfigAsync();
            _logger.LogInformation("已重新加载EMA配置");
            return Ok(new { message = "配置已重新加载", enabled = _monitoringService.IsEnabled() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重新加载EMA配置失败");
            return StatusCode(500, new { message = "重新加载失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 删除配置（用于重置，删除后需重启服务以从appsettings重新初始化）
    /// </summary>
    [HttpDelete]
    public async Task<ActionResult> DeleteConfig()
    {
        try
        {
            await _repository.DeleteConfigAsync();
            _logger.LogInformation("EMA配置已删除");
            return Ok(new { message = "配置已删除，重启服务后将从appsettings重新初始化" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除EMA配置失败");
            return StatusCode(500, new { message = "删除配置失败", error = ex.Message });
        }
    }
}
