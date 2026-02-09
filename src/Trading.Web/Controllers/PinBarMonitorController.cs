using Microsoft.AspNetCore.Mvc;
using Trading.Infrastructure.Models;
using Trading.Infrastructure.Repositories;

namespace Trading.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PinBarMonitorController : ControllerBase
{
    private readonly ILogger<PinBarMonitorController> _logger;
    private readonly IPinBarMonitorRepository _repository;

    public PinBarMonitorController(
        ILogger<PinBarMonitorController> logger,
        IPinBarMonitorRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    /// <summary>
    /// 获取PinBar监控配置
    /// </summary>
    [HttpGet("config")]
    public async Task<ActionResult<PinBarMonitoringConfig>> GetConfig()
    {
        try
        {
            var config = await _repository.GetConfigAsync();
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取PinBar配置失败");
            return StatusCode(500, new { error = "获取配置失败", details = ex.Message });
        }
    }

    /// <summary>
    /// 更新PinBar监控配置
    /// </summary>
    [HttpPost("config")]
    public async Task<ActionResult<PinBarMonitoringConfig>> UpdateConfig([FromBody] PinBarMonitoringConfig config)
    {
        try
        {
            // 调试日志：检查接收到的EMA列表
            _logger.LogInformation("接收到的PinBar配置 - EmaList: [{EmaList}]",
                string.Join(", ", config.StrategySettings.EmaList));

            config.UpdatedBy = "Web";
            var savedConfig = await _repository.SaveConfigAsync(config);

            // 调试日志：检查保存后返回的EMA列表
            _logger.LogInformation("保存后的PinBar配置 - EmaList: [{EmaList}]",
                string.Join(", ", savedConfig.StrategySettings.EmaList));

            _logger.LogInformation("PinBar配置已更新: Enabled={Enabled}, Symbols={Symbols}",
                savedConfig.Enabled, string.Join(",", savedConfig.Symbols));

            return Ok(savedConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新PinBar配置失败");
            return StatusCode(500, new { error = "更新配置失败", details = ex.Message });
        }
    }

    /// <summary>
    /// 启用/禁用PinBar监控
    /// </summary>
    [HttpPost("toggle")]
    public async Task<ActionResult<PinBarMonitoringConfig>> ToggleMonitoring([FromBody] ToggleRequest request)
    {
        try
        {
            var config = await _repository.GetConfigAsync();
            if (config == null)
            {
                return NotFound(new { error = "配置不存在" });
            }

            config.Enabled = request.Enabled;
            config.UpdatedBy = "Web";

            var savedConfig = await _repository.SaveConfigAsync(config);

            _logger.LogInformation("PinBar监控已{Status}", savedConfig.Enabled ? "启用" : "禁用");

            return Ok(savedConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换PinBar监控状态失败");
            return StatusCode(500, new { error = "切换状态失败", details = ex.Message });
        }
    }

    /// <summary>
    /// 获取最近的PinBar信号
    /// </summary>
    [HttpGet("signals")]
    public async Task<ActionResult<List<PinBarSignalHistory>>> GetRecentSignals([FromQuery] int count = 50)
    {
        try
        {
            var signals = await _repository.GetRecentSignalsAsync(count);
            return Ok(signals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取PinBar信号失败");
            return StatusCode(500, new { error = "获取信号失败", details = ex.Message });
        }
    }

    /// <summary>
    /// 获取指定品种的PinBar信号
    /// </summary>
    [HttpGet("signals/{symbol}")]
    public async Task<ActionResult<List<PinBarSignalHistory>>> GetSignalsBySymbol(string symbol, [FromQuery] int count = 50)
    {
        try
        {
            var signals = await _repository.GetSignalsBySymbolAsync(symbol, count);
            return Ok(signals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取品种PinBar信号失败: {Symbol}", symbol);
            return StatusCode(500, new { error = "获取信号失败", details = ex.Message });
        }
    }

    public class ToggleRequest
    {
        public bool Enabled { get; set; }
    }
}
