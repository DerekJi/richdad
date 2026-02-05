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
    private readonly IOandaService? _oandaService;
    private readonly ITelegramService _telegramService;
    private readonly IChartService _chartService;
    private readonly ILogger<SystemController> _logger;

    public SystemController(
        IPriceMonitorService monitorService,
        ITradeLockerService tradeLockerService,
        ITelegramService telegramService,
        IChartService chartService,
        ILogger<SystemController> logger,
        IOandaService? oandaService = null)
    {
        _monitorService = monitorService;
        _tradeLockerService = tradeLockerService;
        _oandaService = oandaService;
        _telegramService = telegramService;
        _chartService = chartService;
        _logger = logger;
    }

    /// <summary>
    /// 测试TradeLocker连接
    /// </summary>
    [HttpPost("test-tradelocker")]
    public async Task<ActionResult> TestTradeLocker()
    {
        var connected = await _tradeLockerService.ConnectAsync();
        if (!connected)
        {
            return BadRequest(new { success = false, message = "TradeLocker连接失败" });
        }

        // 获取账户信息
        var accountInfo = await _tradeLockerService.GetAccountInfoAsync();
        if (accountInfo == null)
        {
            return Ok(new { success = true, message = "TradeLocker连接成功，但无法获取账户信息" });
        }

        return Ok(new
        {
            success = true,
            message = "TradeLocker连接成功",
            account = new
            {
                accountId = accountInfo.AccountId,
                accountName = accountInfo.AccountName,
                balance = accountInfo.Balance,
                equity = accountInfo.Equity,
                margin = accountInfo.Margin,
                freeMargin = accountInfo.FreeMargin,
                currency = accountInfo.Currency
            }
        });
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
    /// 测试OANDA连接
    /// </summary>
    [HttpPost("test-oanda")]
    public async Task<ActionResult> TestOanda()
    {
        if (_oandaService == null)
        {
            return BadRequest(new { success = false, message = "OANDA服务未配置" });
        }

        var connected = await _oandaService.ConnectAsync();
        if (!connected)
        {
            return BadRequest(new { success = false, message = "OANDA连接失败" });
        }

        // 获取账户信息
        var accountInfo = await _oandaService.GetAccountInfoAsync();
        if (accountInfo == null)
        {
            return Ok(new { success = true, message = "OANDA连接成功，但无法获取账户信息" });
        }

        // 测试获取价格
        var price = await _oandaService.GetSymbolPriceAsync("EURUSD");
        
        return Ok(new
        {
            success = true,
            message = "OANDA连接成功",
            accountInfo = new
            {
                accountInfo.AccountId,
                accountInfo.AccountName,
                accountInfo.Balance,
                accountInfo.Currency,
                accountInfo.Equity,
                accountInfo.Margin,
                accountInfo.FreeMargin
            },
            testPrice = price != null ? new
            {
                price.Symbol,
                price.Bid,
                price.Ask
            } : null
        });
    }

    /// <summary>
    /// 测试Telegram连接并发送K线图
    /// </summary>
    [HttpPost("test-chart")]
    public async Task<ActionResult> TestChart([FromQuery] string symbol = "XAUUSD")
    {
        try
        {
            _logger.LogInformation("开始测试K线图生成和发送: {Symbol}", symbol);

            // 连接TradeLocker
            var connected = await _tradeLockerService.ConnectAsync();
            if (!connected)
            {
                return BadRequest(new { success = false, message = "TradeLocker连接失败" });
            }

            // 获取4个时间周期的K线数据
            _logger.LogInformation("获取 {Symbol} 的历史数据...", symbol);
            var candlesM5 = (await _tradeLockerService.GetHistoricalDataAsync(symbol, "M5", 60))?.ToList();
            var candlesM15 = (await _tradeLockerService.GetHistoricalDataAsync(symbol, "M15", 60))?.ToList();
            var candlesH1 = (await _tradeLockerService.GetHistoricalDataAsync(symbol, "H1", 60))?.ToList();
            var candlesH4 = (await _tradeLockerService.GetHistoricalDataAsync(symbol, "H4", 60))?.ToList();

            // 验证数据
            _logger.LogInformation("数据统计: M5={M5Count}, M15={M15Count}, H1={H1Count}, H4={H4Count}",
                candlesM5?.Count ?? 0,
                candlesM15?.Count ?? 0,
                candlesH1?.Count ?? 0,
                candlesH4?.Count ?? 0);

            if (candlesM5 == null || candlesM5.Count == 0)
            {
                return BadRequest(new { success = false, message = $"无法获取 {symbol} 的 M5 周期数据，请检查品种名称是否正确" });
            }
            if (candlesM15 == null || candlesM15.Count == 0)
            {
                return BadRequest(new { success = false, message = $"无法获取 {symbol} 的 M15 周期数据" });
            }
            if (candlesH1 == null || candlesH1.Count == 0)
            {
                return BadRequest(new { success = false, message = $"无法获取 {symbol} 的 H1 周期数据" });
            }
            if (candlesH4 == null || candlesH4.Count == 0)
            {
                return BadRequest(new { success = false, message = $"无法获取 {symbol} 的 H4 周期数据" });
            }

            _logger.LogInformation("开始生成K线图...");

            // 生成图表
            using var chartStream = await _chartService.GenerateMultiTimeFrameChartAsync(
                symbol,
                candlesM5,
                candlesM15,
                candlesH1,
                candlesH4,
                20  // EMA20
            );

            _logger.LogInformation("K线图生成成功，准备发送到Telegram...");

            // 发送到Telegram
            var caption = $"📊 {symbol} K线图测试\n\n包含4个时间周期（M5, M15, H1, H4）的K线图和EMA20";
            var sent = await _telegramService.SendPhotoAsync(chartStream, caption);

            if (sent)
            {
                _logger.LogInformation("K线图已成功发送到Telegram");
                return Ok(new { success = true, message = "K线图已生成并发送到Telegram" });
            }
            else
            {
                return StatusCode(500, new { success = false, message = "发送到Telegram失败" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试K线图失败");
            return StatusCode(500, new { success = false, message = $"测试失败: {ex.Message}" });
        }
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
