using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Trading.Backtest.Services;
using Trading.Data.Configuration;
using Trading.Data.Models;

namespace Trading.Backtest.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BacktestController : ControllerBase
{
    private readonly AppSettings _appSettings;
    private readonly BacktestRunner _runner;
    private readonly IConfiguration _configuration;

    public BacktestController(
        AppSettings appSettings,
        BacktestRunner runner,
        IConfiguration configuration)
    {
        _appSettings = appSettings;
        _runner = runner;
        _configuration = configuration;
    }

    /// <summary>
    /// 获取所有策略配置
    /// </summary>
    [HttpGet("strategies")]
    public IActionResult GetStrategies()
    {
        try
        {
            if (_appSettings?.Strategies == null || _appSettings.Strategies.Count == 0)
            {
                return Ok(new List<object>()); // 返回空数组而不是错误
            }
            
            var strategies = _appSettings.Strategies.Keys
                .Select(name => new
                {
                    Name = name,
                    Config = _appSettings.Strategies[name].ToStrategyConfig(
                        name,
                        _appSettings.Indicators.BaseEma,
                        _appSettings.Indicators.AtrPeriod)
                })
                .ToList();

            return Ok(strategies);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// 获取指定策略配置
    /// </summary>
    [HttpGet("strategies/{name}")]
    public IActionResult GetStrategy(string name)
    {
        if (!_appSettings.Strategies.ContainsKey(name))
        {
            return NotFound(new { error = $"策略 '{name}' 不存在" });
        }

        var strategySettings = _appSettings.Strategies[name];
        var config = strategySettings.ToStrategyConfig(
            name, 
            _appSettings.Indicators.BaseEma,
            _appSettings.Indicators.AtrPeriod);
            
        return Ok(new
        {
            Name = name,
            Config = config,
            Account = _appSettings.Account,
            Indicators = _appSettings.Indicators
        });
    }

    /// <summary>
    /// 运行回测
    /// </summary>
    [HttpPost("run")]
    public async Task<IActionResult> RunBacktest([FromBody] BacktestRequest request)
    {
        try
        {
            // 获取数据目录
            var dataPath = _configuration["DataPath"] ?? "..\\..\\..\\..\\..\\data";
            var dataDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, dataPath));
            
            // 构建策略配置
            var config = new StrategyConfig
            {
                StrategyName = request.StrategyName,
                Symbol = request.Symbol,
                CsvFilter = request.CsvFilter ?? string.Empty,
                ContractSize = request.Symbol == "XAUUSD" ? 100 : request.Symbol == "XAGUSD" ? 1000 : 100,
                BaseEma = request.BaseEma,
                AtrPeriod = request.AtrPeriod,
                Threshold = request.Threshold,
                MaxBodyPercentage = request.MaxBodyPercentage,
                MinLongerWickPercentage = request.MinLongerWickPercentage,
                MaxShorterWickPercentage = request.MaxShorterWickPercentage,
                EmaList = request.EmaList ?? new List<int>(),
                NearEmaThreshold = request.NearEmaThreshold,
                RiskRewardRatio = request.RiskRewardRatio,
                StopLossAtrRatio = request.StopLossAtrRatio,
                StartTradingHour = request.StartTradingHour,
                EndTradingHour = request.EndTradingHour,
                NoTradingHoursLimit = request.NoTradingHoursLimit,
                RequirePinBarDirectionMatch = request.RequirePinBarDirectionMatch,
                MinLowerWickAtrRatio = request.MinLowerWickAtrRatio
            };

            var accountSettings = new AccountSettings
            {
                InitialCapital = request.InitialCapital,
                Leverage = request.Leverage,
                MaxLossPerTradePercent = request.MaxLossPerTradePercent,
                MaxDailyLossPercent = request.MaxDailyLossPercent
            };

            // 运行回测
            var result = await _runner.RunAsync(config, accountSettings, dataDirectory);

            // 返回结果（包含用于图表的数据）
            return Ok(new
            {
                Config = config,
                Account = accountSettings,
                Result = new
                {
                    result.Id,
                    result.StartTime,
                    result.EndTime,
                    OverallMetrics = new
                    {
                        result.OverallMetrics.TotalTrades,
                        result.OverallMetrics.WinningTrades,
                        result.OverallMetrics.LosingTrades,
                        result.OverallMetrics.WinRate,
                        result.OverallMetrics.TotalProfit,
                        result.OverallMetrics.TotalReturnRate,
                        result.OverallMetrics.AverageHoldingTime,
                        // 从交易列表计算平均盈利和亏损
                        AvgWin = result.Trades.Where(t => t.ProfitLoss > 0).Any() 
                            ? result.Trades.Where(t => t.ProfitLoss > 0).Average(t => t.ProfitLoss ?? 0)
                            : 0,
                        AvgLoss = result.Trades.Where(t => t.ProfitLoss < 0).Any()
                            ? result.Trades.Where(t => t.ProfitLoss < 0).Average(t => t.ProfitLoss ?? 0)
                            : 0,
                        SharpeRatio = 0m, // 夏普比率：风险调整后的收益率，需要收益率序列计算
                        result.OverallMetrics.MaxConsecutiveWins,
                        MaxConsecutiveWinsStartTime = result.OverallMetrics.MaxConsecutiveWinsStartTime?.ToString("yyyy-MM-dd"),
                        MaxConsecutiveWinsEndTime = result.OverallMetrics.MaxConsecutiveWinsEndTime?.ToString("yyyy-MM-dd"),
                        result.OverallMetrics.MaxConsecutiveLosses,
                        MaxConsecutiveLossesStartTime = result.OverallMetrics.MaxConsecutiveLossesStartTime?.ToString("yyyy-MM-dd"),
                        MaxConsecutiveLossesEndTime = result.OverallMetrics.MaxConsecutiveLossesEndTime?.ToString("yyyy-MM-dd"),
                        result.OverallMetrics.MaxDrawdown,
                        MaxDrawdownStartTime = result.OverallMetrics.MaxDrawdownStartTime?.ToString("yyyy-MM-dd"),
                        MaxDrawdownEndTime = result.OverallMetrics.MaxDrawdownEndTime?.ToString("yyyy-MM-dd"),
                        result.OverallMetrics.ProfitFactor,
                        result.OverallMetrics.AverageTradesPerMonth
                    },
                    YearlyMetrics = result.YearlyMetrics.Select(y => new
                    {
                        y.Period,
                        y.StartDate,
                        y.EndDate,
                        y.TradeCount,
                        y.WinningTrades,
                        y.WinRate,
                        y.ProfitLoss
                    }),
                    result.MonthlyMetrics,
                    result.WeeklyMetrics,
                    EquityCurve = result.EquityCurve.Select(p => new
                    {
                        Time = p.Time.ToString("yyyy-MM-dd HH:mm"),
                        p.CumulativeProfit,
                        p.CumulativeReturnRate
                    }),
                    AllTrades = result.Trades.Select(t => new
                    {
                        t.Id,
                        Direction = t.Direction.ToString(),
                        OpenTime = t.OpenTime.ToString("yyyy-MM-dd HH:mm"),
                        t.OpenPrice,
                        t.StopLoss,
                        t.TakeProfit,
                        StopLossPips = t.StopLossPips,
                        TakeProfitPips = t.TakeProfitPips,
                        CloseTime = t.CloseTime?.ToString("yyyy-MM-dd HH:mm"),
                        t.ClosePrice,
                        CloseReason = t.CloseReason?.ToString() ?? "",
                        t.ProfitLoss,
                        t.ReturnRate,
                        Lots = 1 // 暂时固定为1手，后续可以从配置中获取
                    })
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message, stack = ex.StackTrace });
        }
    }
}

/// <summary>
/// 回测请求参数
/// </summary>
public class BacktestRequest
{
    public string StrategyName { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string? CsvFilter { get; set; }
    public int BaseEma { get; set; }
    public int AtrPeriod { get; set; }
    public decimal Threshold { get; set; }
    public decimal MaxBodyPercentage { get; set; }
    public decimal MinLongerWickPercentage { get; set; }
    public decimal MaxShorterWickPercentage { get; set; }
    public List<int>? EmaList { get; set; }
    public decimal NearEmaThreshold { get; set; }
    public decimal RiskRewardRatio { get; set; }
    public decimal StopLossAtrRatio { get; set; }
    public int StartTradingHour { get; set; }
    public int EndTradingHour { get; set; }
    public bool NoTradingHoursLimit { get; set; }
    public bool RequirePinBarDirectionMatch { get; set; }
    public decimal MinLowerWickAtrRatio { get; set; }
    
    // Account settings
    public double InitialCapital { get; set; } = 100000;
    public double Leverage { get; set; } = 30;
    public double MaxLossPerTradePercent { get; set; } = 0.5;
    public double MaxDailyLossPercent { get; set; } = 3.0;
}
