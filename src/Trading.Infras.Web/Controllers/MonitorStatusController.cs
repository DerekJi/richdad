using Microsoft.AspNetCore.Mvc;
using Trading.AlertSystem.Data.Models;
using Trading.AlertSystem.Data.Repositories;
using Trading.AlertSystem.Data.Services;
using Trading.AlertSystem.Service.Repositories;
using Trading.AlertSystem.Service.Services;

namespace Trading.AlertSystem.Web.Controllers;

/// <summary>
/// 监控状态API控制器 - 获取所有有效监控规则的实时状态
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MonitorStatusController : ControllerBase
{
    private readonly IPriceMonitorRepository _priceMonitorRepository;
    private readonly IEmaMonitoringService _emaMonitoringService;
    private readonly IEmaMonitorRepository _emaMonitorRepository;
    private readonly IMarketDataService _marketDataService;
    private readonly IPriceCacheService _priceCacheService;
    private readonly ILogger<MonitorStatusController> _logger;

    public MonitorStatusController(
        IPriceMonitorRepository priceMonitorRepository,
        IEmaMonitoringService emaMonitoringService,
        IEmaMonitorRepository emaMonitorRepository,
        IMarketDataService marketDataService,
        IPriceCacheService priceCacheService,
        ILogger<MonitorStatusController> logger)
    {
        _priceMonitorRepository = priceMonitorRepository;
        _emaMonitoringService = emaMonitoringService;
        _emaMonitorRepository = emaMonitorRepository;
        _marketDataService = marketDataService;
        _priceCacheService = priceCacheService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有有效监控规则的实时状态
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var result = new List<MonitorStatusItem>();

            // 1. 获取价格监控规则
            var priceRules = await _priceMonitorRepository.GetEnabledRulesAsync();
            var symbols = priceRules.Select(r => r.Symbol).Distinct().ToList();

            // 2. 获取EMA配置和已有状态
            var emaConfig = await _emaMonitorRepository.GetConfigAsync();
            var emaStates = (await _emaMonitoringService.GetStatesAsync()).ToDictionary(s => s.Id, s => s);

            // 收集 EMA 监控的品种
            var emaSymbols = emaConfig?.Enabled == true ? emaConfig.Symbols : new List<string>();

            // 合并所有需要查询价格的品种
            var allSymbols = symbols.Union(emaSymbols).Distinct().ToList();

            // 3. 获取当前价格（优先使用缓存，避免频繁请求数据源）
            var prices = new Dictionary<string, decimal>();
            var symbolsNeedFetch = new List<string>();

            // 先检查缓存
            foreach (var symbol in allSymbols)
            {
                var cachedPrice = _priceCacheService.GetCachedPrice(symbol);
                if (cachedPrice != null)
                {
                    var priceValue = cachedPrice.LastPrice > 0 ? cachedPrice.LastPrice : (cachedPrice.Bid + cachedPrice.Ask) / 2;
                    if (priceValue > 0)
                    {
                        prices[symbol] = priceValue;
                    }
                }
                else
                {
                    symbolsNeedFetch.Add(symbol);
                }
            }

            // 只请求缓存中没有的品种
            if (symbolsNeedFetch.Count > 0)
            {
                _logger.LogDebug("从数据源获取 {Count} 个品种价格: {Symbols}", symbolsNeedFetch.Count, string.Join(", ", symbolsNeedFetch));

                foreach (var symbol in symbolsNeedFetch)
                {
                    try
                    {
                        var price = await _marketDataService.GetSymbolPriceAsync(symbol);
                        if (price != null)
                        {
                            // 更新缓存
                            _priceCacheService.UpdatePrice(symbol, price);

                            var priceValue = price.LastPrice > 0 ? price.LastPrice : (price.Bid + price.Ask) / 2;
                            if (priceValue > 0)
                            {
                                prices[symbol] = priceValue;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "获取 {Symbol} 价格失败", symbol);
                    }
                }
            }

            // 4. 构建价格监控状态
            foreach (var rule in priceRules)
            {
                var currentPrice = prices.GetValueOrDefault(rule.Symbol, 0);
                var targetPrice = rule.TargetPrice ?? 0;
                var distance = targetPrice > 0 ? (currentPrice - targetPrice) / targetPrice * 100 : 0;

                result.Add(new MonitorStatusItem
                {
                    Id = rule.Id,
                    Type = "PriceMonitor",
                    Symbol = rule.Symbol,
                    Name = rule.Name,
                    CurrentPrice = currentPrice,
                    TargetPrice = targetPrice,
                    Direction = rule.Direction == PriceDirection.Above ? "上穿" : "下穿",
                    Distance = Math.Round(distance, 4),
                    TimeFrame = rule.TimeFrame,
                    LastCheckTime = DateTime.UtcNow
                });
            }

            // 5. 构建EMA监控状态 - 从配置生成所有组合
            if (emaConfig?.Enabled == true)
            {
                foreach (var symbol in emaConfig.Symbols)
                {
                    foreach (var timeFrame in emaConfig.TimeFrames)
                    {
                        foreach (var emaPeriod in emaConfig.EmaPeriods)
                        {
                            var stateId = $"{symbol}_{timeFrame}_EMA{emaPeriod}";

                            // 尝试获取已计算的状态
                            emaStates.TryGetValue(stateId, out var state);

                            // 使用实时价格，或状态中的收盘价，或0
                            var currentPrice = prices.TryGetValue(symbol, out var livePrice) && livePrice > 0
                                ? livePrice
                                : (state?.LastClose ?? 0);

                            var emaValue = state?.LastEmaValue ?? 0;
                            var position = state?.LastPosition ?? 0;

                            result.Add(new MonitorStatusItem
                            {
                                Id = stateId,
                                Type = "EmaMonitor",
                                Symbol = symbol,
                                Name = $"EMA{emaPeriod}",
                                CurrentPrice = currentPrice,
                                TargetPrice = emaValue,
                                Direction = position == 0 ? "等待计算" : (position > 0 ? "价格在EMA上方" : "价格在EMA下方"),
                                Distance = emaValue > 0 && currentPrice > 0
                                    ? Math.Round((currentPrice - emaValue) / emaValue * 100, 4)
                                    : 0,
                                TimeFrame = timeFrame,
                                EmaPeriod = emaPeriod,
                                LastCheckTime = state?.LastCheckTime ?? DateTime.MinValue
                            });
                        }
                    }
                }
            }

            // 按品种和类型排序
            result = result.OrderBy(r => r.Symbol).ThenBy(r => r.Type).ThenBy(r => r.TimeFrame).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取监控状态失败");
            return StatusCode(500, new { Error = "获取监控状态失败" });
        }
    }

    /// <summary>
    /// 刷新指定品种的价格
    /// </summary>
    [HttpGet("price/{symbol}")]
    public async Task<IActionResult> GetPrice(string symbol)
    {
        try
        {
            var price = await _marketDataService.GetSymbolPriceAsync(symbol);
            if (price == null)
            {
                return NotFound(new { Error = $"无法获取 {symbol} 的价格" });
            }

            return Ok(new
            {
                Symbol = symbol,
                Price = price.LastPrice,
                Bid = price.Bid,
                Ask = price.Ask,
                Time = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 {Symbol} 价格失败", symbol);
            return StatusCode(500, new { Error = "获取价格失败" });
        }
    }
}

/// <summary>
/// 监控状态项
/// </summary>
public class MonitorStatusItem
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal TargetPrice { get; set; }
    public string Direction { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public string TimeFrame { get; set; } = string.Empty;
    public int? EmaPeriod { get; set; }
    public DateTime LastCheckTime { get; set; }
}
