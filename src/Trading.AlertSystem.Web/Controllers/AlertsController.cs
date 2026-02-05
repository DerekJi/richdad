using Microsoft.AspNetCore.Mvc;
using Trading.AlertSystem.Data.Models;
using Trading.AlertSystem.Service.Repositories;
using Trading.AlertSystem.Service.Services;

namespace Trading.AlertSystem.Web.Controllers;

/// <summary>
/// 价格告警管理API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AlertsController : ControllerBase
{
    private readonly IPriceAlertRepository _alertRepository;
    private readonly IStreamingPriceMonitorService? _streamingMonitor;
    private readonly ILogger<AlertsController> _logger;

    public AlertsController(
        IPriceAlertRepository alertRepository,
        IStreamingPriceMonitorService? streamingMonitor,
        ILogger<AlertsController> logger)
    {
        _alertRepository = alertRepository;
        _streamingMonitor = streamingMonitor;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有告警
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PriceAlert>>> GetAll()
    {
        var alerts = await _alertRepository.GetAllAsync();
        return Ok(alerts);
    }

    /// <summary>
    /// 获取启用的告警
    /// </summary>
    [HttpGet("enabled")]
    public async Task<ActionResult<IEnumerable<PriceAlert>>> GetEnabled()
    {
        var alerts = await _alertRepository.GetEnabledAlertsAsync();
        return Ok(alerts);
    }

    /// <summary>
    /// 根据ID获取告警
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PriceAlert>> GetById(string id)
    {
        var alert = await _alertRepository.GetByIdAsync(id);
        if (alert == null)
            return NotFound();

        return Ok(alert);
    }

    /// <summary>
    /// 创建新告警
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PriceAlert>> Create([FromBody] CreateAlertRequest request)
    {
        try
        {
            var alert = new PriceAlert
            {
                Symbol = request.Symbol,
                Name = request.Name,
                Type = request.Type,
                TargetPrice = request.TargetPrice,
                Direction = request.Direction,
                EmaPeriod = request.EmaPeriod,
                MaPeriod = request.MaPeriod,
                TimeFrame = request.TimeFrame ?? "M5",
                MessageTemplate = request.MessageTemplate ?? string.Empty,
                TelegramChatId = request.TelegramChatId,
                Enabled = request.Enabled
            };

            var created = await _alertRepository.CreateAsync(alert);

            // 刷新 Streaming 订阅
            if (_streamingMonitor != null)
            {
                _ = _streamingMonitor.RefreshAlertsAsync();
            }

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建告警失败");
            return StatusCode(500, "创建告警失败");
        }
    }

    /// <summary>
    /// 更新告警
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<PriceAlert>> Update(string id, [FromBody] UpdateAlertRequest request)
    {
        try
        {
            var existing = await _alertRepository.GetByIdAsync(id);
            if (existing == null)
                return NotFound();

            existing.Symbol = request.Symbol ?? existing.Symbol;
            existing.Name = request.Name ?? existing.Name;
            existing.Type = request.Type ?? existing.Type;
            existing.TargetPrice = request.TargetPrice ?? existing.TargetPrice;
            existing.Direction = request.Direction ?? existing.Direction;
            existing.EmaPeriod = request.EmaPeriod ?? existing.EmaPeriod;
            existing.MaPeriod = request.MaPeriod ?? existing.MaPeriod;
            existing.TimeFrame = request.TimeFrame ?? existing.TimeFrame;
            existing.MessageTemplate = request.MessageTemplate ?? existing.MessageTemplate;
            existing.TelegramChatId = request.TelegramChatId ?? existing.TelegramChatId;
            existing.Enabled = request.Enabled ?? existing.Enabled;

            var updated = await _alertRepository.UpdateAsync(existing);

            // 刷新 Streaming 订阅
            if (_streamingMonitor != null)
            {
                _ = _streamingMonitor.RefreshAlertsAsync();
            }

            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新告警失败");
            return StatusCode(500, "更新告警失败");
        }
    }

    /// <summary>
    /// 删除告警
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var success = await _alertRepository.DeleteAsync(id);
        if (!success)
            return NotFound();

        // 刷新 Streaming 订阅
        if (_streamingMonitor != null)
        {
            _ = _streamingMonitor.RefreshAlertsAsync();
        }

        return NoContent();
    }

    /// <summary>
    /// 重置告警状态
    /// </summary>
    [HttpPost("{id}/reset")]
    public async Task<ActionResult> Reset(string id)
    {
        var alert = await _alertRepository.GetByIdAsync(id);
        if (alert == null)
            return NotFound();

        await _alertRepository.ResetAlertAsync(id);
        return Ok();
    }
}

/// <summary>
/// 创建告警请求
/// </summary>
public class CreateAlertRequest
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AlertType Type { get; set; }
    public decimal? TargetPrice { get; set; }
    public PriceDirection Direction { get; set; }
    public int? EmaPeriod { get; set; }
    public int? MaPeriod { get; set; }
    public string? TimeFrame { get; set; }
    public string? MessageTemplate { get; set; }
    public long? TelegramChatId { get; set; }
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// 更新告警请求
/// </summary>
public class UpdateAlertRequest
{
    public string? Symbol { get; set; }
    public string? Name { get; set; }
    public AlertType? Type { get; set; }
    public decimal? TargetPrice { get; set; }
    public PriceDirection? Direction { get; set; }
    public int? EmaPeriod { get; set; }
    public int? MaPeriod { get; set; }
    public string? TimeFrame { get; set; }
    public string? MessageTemplate { get; set; }
    public long? TelegramChatId { get; set; }
    public bool? Enabled { get; set; }
}
