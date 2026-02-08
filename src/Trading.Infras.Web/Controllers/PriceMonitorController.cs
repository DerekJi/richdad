using Microsoft.AspNetCore.Mvc;
using Trading.Infras.Data.Models;
using Trading.Infras.Service.Repositories;
using Trading.Infras.Service.Services;

namespace Trading.Infras.Web.Controllers;

/// <summary>
/// 价格监控管理API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PriceMonitorController : ControllerBase
{
    private readonly IPriceMonitorRepository _repository;
    private readonly IStreamingPriceMonitorService? _streamingMonitor;
    private readonly ILogger<PriceMonitorController> _logger;

    public PriceMonitorController(
        IPriceMonitorRepository repository,
        IStreamingPriceMonitorService? streamingMonitor,
        ILogger<PriceMonitorController> logger)
    {
        _repository = repository;
        _streamingMonitor = streamingMonitor;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有监控规则
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PriceMonitorRule>>> GetAll()
    {
        var rules = await _repository.GetAllAsync();
        return Ok(rules);
    }

    /// <summary>
    /// 获取启用的监控规则
    /// </summary>
    [HttpGet("enabled")]
    public async Task<ActionResult<IEnumerable<PriceMonitorRule>>> GetEnabled()
    {
        var rules = await _repository.GetEnabledRulesAsync();
        return Ok(rules);
    }

    /// <summary>
    /// 根据ID获取监控规则
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PriceMonitorRule>> GetById(string id)
    {
        var rule = await _repository.GetByIdAsync(id);
        if (rule == null)
            return NotFound();

        return Ok(rule);
    }

    /// <summary>
    /// 创建新监控规则
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PriceMonitorRule>> Create([FromBody] CreateRuleRequest request)
    {
        try
        {
            var rule = new PriceMonitorRule
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

            var created = await _repository.CreateAsync(rule);

            // 刷新 Streaming 订阅
            if (_streamingMonitor != null)
            {
                _ = _streamingMonitor.RefreshAlertsAsync();
            }

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建监控规则失败");
            return StatusCode(500, "创建监控规则失败");
        }
    }

    /// <summary>
    /// 更新监控规则
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<PriceMonitorRule>> Update(string id, [FromBody] UpdateRuleRequest request)
    {
        try
        {
            var existing = await _repository.GetByIdAsync(id);
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

            var updated = await _repository.UpdateAsync(existing);

            // 刷新 Streaming 订阅
            if (_streamingMonitor != null)
            {
                _ = _streamingMonitor.RefreshAlertsAsync();
            }

            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新监控规则失败");
            return StatusCode(500, "更新监控规则失败");
        }
    }

    /// <summary>
    /// 删除监控规则
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var success = await _repository.DeleteAsync(id);
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
    /// 重置监控规则状态
    /// </summary>
    [HttpPost("{id}/reset")]
    public async Task<ActionResult> Reset(string id)
    {
        var rule = await _repository.GetByIdAsync(id);
        if (rule == null)
            return NotFound();

        await _repository.ResetRuleAsync(id);
        return Ok();
    }
}

/// <summary>
/// 创建监控规则请求
/// </summary>
public class CreateRuleRequest
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
/// 更新监控规则请求
/// </summary>
public class UpdateRuleRequest
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
