using Microsoft.AspNetCore.Mvc;
using Trading.AlertSystem.Data.Services;
using Trading.AlertSystem.Service.Services;

namespace Trading.AlertSystem.Web.Controllers;

/// <summary>
/// 系统监控与测试API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SystemController : ControllerBase
{
    private readonly IPriceMonitorService _monitorService;
    private readonly ITradeLockerService _tradeLockerService;
    private readonly ITelegramService _telegramService;
    private readonly ILogger<SystemController> _logger;

    public SystemController(
        IPriceMonitorService monitorService,
        ITradeLockerService tradeLockerService,
        ITelegramService telegramService,
        ILogger<SystemController> logger)
    {
        _monitorService = monitorService;
        _tradeLockerService = tradeLockerService;
        _telegramService = telegramService;
        _logger = logger;
    }

    /// <summary>
    /// 测试TradeLocker连接
    /// </summary>
    [HttpPost("test-tradelocker")]
    public async Task<ActionResult> TestTradeLocker()
    {
        var connected = await _tradeLockerService.ConnectAsync();
        if (connected)
            return Ok(new { success = true, message = "TradeLocker连接成功" });
        
        return BadRequest(new { success = false, message = "TradeLocker连接失败" });
    }

    /// <summary>
    /// 测试Telegram连接
    /// </summary>
    [HttpPost("test-telegram")]
    public async Task<ActionResult> TestTelegram()
    {
        var connected = await _telegramService.TestConnectionAsync();
        if (connected)
        {
            await _telegramService.SendMessageAsync("✅ Telegram连接测试成功！");
            return Ok(new { success = true, message = "Telegram连接成功，已发送测试消息" });
        }
        
        return BadRequest(new { success = false, message = "Telegram连接失败" });
    }

    /// <summary>
    /// 手动触发一次监控检查
    /// </summary>
    [HttpPost("check-now")]
    public async Task<ActionResult> CheckNow()
    {
        try
        {
            await _monitorService.ExecuteCheckAsync();
            return Ok(new { success = true, message = "已执行监控检查" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行监控检查失败");
            return StatusCode(500, new { success = false, message = "执行监控检查失败" });
        }
    }

    /// <summary>
    /// 获取指定品种的实时价格
    /// </summary>
    [HttpGet("price/{symbol}")]
    public async Task<ActionResult> GetPrice(string symbol)
    {
        var price = await _tradeLockerService.GetSymbolPriceAsync(symbol);
        if (price == null)
            return NotFound(new { success = false, message = $"无法获取{symbol}的价格" });

        return Ok(price);
    }

    /// <summary>
    /// 系统健康检查
    /// </summary>
    [HttpGet("health")]
    public ActionResult Health()
    {
        return Ok(new 
        { 
            status = "healthy", 
            timestamp = DateTime.UtcNow,
            service = "Trading Alert System"
        });
    }
}
