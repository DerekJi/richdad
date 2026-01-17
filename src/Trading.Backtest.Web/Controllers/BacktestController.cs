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
                NoTradeHours = request.NoTradeHours ?? new List<int>(),
                RequirePinBarDirectionMatch = request.RequirePinBarDirectionMatch,
                MinLowerWickAtrRatio = request.MinLowerWickAtrRatio,
                MinAdx = request.MinAdx,
                AdxPeriod = request.AdxPeriod,
                AdxTimeframe = Enum.TryParse<AdxTimeframe>(request.AdxTimeframe, out var timeframe) ? timeframe : AdxTimeframe.Current,
                LowAdxRiskRewardRatio = request.LowAdxRiskRewardRatio,
                MaxConsecutiveLosses = request.MaxConsecutiveLosses,
                PauseDaysAfterLosses = request.PauseDaysAfterLosses
            };

            var accountSettings = new AccountSettings
            {
                InitialCapital = request.InitialCapital,
                Leverage = request.Leverage,
                MaxLossPerTradePercent = request.MaxLossPerTradePercent,
                MaxDailyLossPercent = request.MaxDailyLossPercent,
                EnableDynamicRiskManagement = request.EnableDynamicRiskManagement
            };

            // 解析日期范围
            DateTime? startDate = null;
            DateTime? endDate = null;
            if (!string.IsNullOrEmpty(request.StartDate))
            {
                startDate = DateTime.Parse(request.StartDate);
            }
            if (!string.IsNullOrEmpty(request.EndDate))
            {
                endDate = DateTime.Parse(request.EndDate);
            }

            // 运行回测
            var result = await _runner.RunAsync(config, accountSettings, dataDirectory, startDate, endDate);

            // 获取CSV文件名
            var dataProvider = new Trading.Data.Providers.CsvDataProvider(dataDirectory);
            var csvFilePath = dataProvider.FindCsvFile(config.Symbol, config.CsvFilter);
            var csvFileName = !string.IsNullOrEmpty(csvFilePath) ? System.IO.Path.GetFileName(csvFilePath) : "N/A";

            // 计算时间段分析
            var topProfitSlots = TimeSlotAnalyzer.GetTopProfitableSlots(result.Trades, 5);
            var topLossSlots = TimeSlotAnalyzer.GetTopLossSlots(result.Trades, 5);

            // 返回结果（包含用于图表的数据）
            return Ok(new
            {
                Config = config,
                Account = accountSettings,
                CsvFileName = csvFileName,
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
                        y.ProfitLoss,
                        y.ReturnRate
                    }),
                    MonthlyMetrics = result.MonthlyMetrics.Select(m => new
                    {
                        m.Period,
                        m.StartDate,
                        m.EndDate,
                        m.TradeCount,
                        m.WinningTrades,
                        m.WinRate,
                        m.ProfitLoss,
                        m.ReturnRate
                    }),
                    result.WeeklyMetrics,
                    TopProfitSlots = topProfitSlots.Select(s => new
                    {
                        s.TimeSlot,
                        s.TradeCount,
                        s.TotalProfitLoss,
                        s.AvgProfitLoss,
                        s.WinRate
                    }),
                    TopLossSlots = topLossSlots.Select(s => new
                    {
                        s.TimeSlot,
                        s.TradeCount,
                        s.TotalProfitLoss,
                        s.AvgProfitLoss,
                        s.WinRate
                    }),
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

    /// <summary>
    /// 获取指定交易的K线数据
    /// </summary>
    [HttpPost("trade-klines")]
    public IActionResult GetTradeKlines([FromBody] TradeKlineRequest request)
    {
        try
        {
            // 获取数据目录
            var dataPath = _configuration["DataPath"] ?? "..\\..\\..\\..\\..\\data";
            var dataDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, dataPath));

            // 加载CSV数据
            var candles = _runner.LoadCandlesFromCsv(dataDirectory, request.Symbol, request.CsvFilter ?? string.Empty);
            if (candles == null || candles.Count == 0)
            {
                return BadRequest(new { error = "无法加载K线数据" });
            }

            // 找到开仓和平仓时间对应的K线索引
            var openTime = DateTime.Parse(request.OpenTime);
            var closeTime = request.CloseTime != null ? DateTime.Parse(request.CloseTime) : openTime;

            var openIndex = candles.FindIndex(c => c.DateTime >= openTime);
            var closeIndex = candles.FindIndex(c => c.DateTime >= closeTime);

            if (openIndex == -1 || closeIndex == -1)
            {
                return BadRequest(new { error = "找不到对应的K线数据" });
            }

            // 计算范围：开仓前200根到平仓后50根
            var startIndex = Math.Max(0, openIndex - 200);
            var endIndex = Math.Min(candles.Count - 1, closeIndex + 50);
            var rangeCandles = candles.GetRange(startIndex, endIndex - startIndex + 1);

            // 计算EMA
            var emaList = request.EmaList ?? new List<int> { 20, 60, 80, 100, 200 };
            var indicatorCalculator = new Trading.Core.Indicators.IndicatorCalculator();

            // 为了计算EMA，需要从更早的K线开始（至少需要EMA周期的2-3倍数据）
            var maxEmaPeriod = emaList.Max();
            var emaStartIndex = Math.Max(0, startIndex - maxEmaPeriod * 3);
            var emaCandles = candles.GetRange(emaStartIndex, endIndex - emaStartIndex + 1);

            var config = new StrategyConfig
            {
                BaseEma = request.BaseEma,
                AtrPeriod = request.AtrPeriod,
                EmaList = emaList,
                AdxPeriod = request.AdxPeriod,
                AdxTimeframe = Enum.TryParse<AdxTimeframe>(request.AdxTimeframe, out var adxTimeframe) ? adxTimeframe : AdxTimeframe.Current
            };
            indicatorCalculator.CalculateIndicators(emaCandles, config);

            // 只返回需要显示的K线范围（开仓前200根到平仓后50根）
            var displayStartIndex = startIndex - emaStartIndex;
            var displayCandles = emaCandles.Skip(displayStartIndex).ToList();

            // 构造响应数据
            var response = new
            {
                Candles = displayCandles.Select(c => new
                {
                    DateTime = c.DateTime.ToString("yyyy-MM-dd HH:mm"),
                    Open = c.Open,
                    High = c.High,
                    Low = c.Low,
                    Close = c.Close,
                    Atr = c.ATR,
                    Adx = c.ADX
                }).ToList(),
                EmaData = emaList.ToDictionary(
                    period => period,
                    period => displayCandles.Select(c =>
                    {
                        var ema = Trading.Core.Indicators.IndicatorCalculator.GetEma(c, period);
                        return ema > 0 ? (decimal?)ema : null;
                    }).ToList()
                ),
                AdxData = displayCandles.Select(c => c.ADX > 0 ? (decimal?)c.ADX : null).ToList(),
                OpenIndex = openIndex - startIndex,
                CloseIndex = closeIndex - startIndex,
                OpenPrice = request.OpenPrice,
                ClosePrice = request.ClosePrice,
                StopLoss = request.StopLoss,
                TakeProfit = request.TakeProfit,
                Direction = request.Direction
            };

            return Ok(response);
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
    public List<int>? NoTradeHours { get; set; }
    public bool RequirePinBarDirectionMatch { get; set; }
    public decimal MinLowerWickAtrRatio { get; set; }
    public decimal MinAdx { get; set; } = 0;
    public int AdxPeriod { get; set; } = 14;
    public string AdxTimeframe { get; set; } = "Current";
    public decimal LowAdxRiskRewardRatio { get; set; } = 0;
    public int MaxConsecutiveLosses { get; set; } = 0;
    public int PauseDaysAfterLosses { get; set; } = 5;

    // Account settings
    public double InitialCapital { get; set; } = 100000;
    public double Leverage { get; set; } = 30;
    public double MaxLossPerTradePercent { get; set; } = 0.5;
    public double MaxDailyLossPercent { get; set; } = 3.0;
    public bool EnableDynamicRiskManagement { get; set; }

    // Date range filter
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
}

/// <summary>
/// 获取交易K线请求参数
/// </summary>
public class TradeKlineRequest
{
    public string Symbol { get; set; } = string.Empty;
    public string? CsvFilter { get; set; }
    public string OpenTime { get; set; } = string.Empty;
    public string? CloseTime { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal? ClosePrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal TakeProfit { get; set; }
    public string Direction { get; set; } = string.Empty;
    public int BaseEma { get; set; } = 200;
    public int AtrPeriod { get; set; } = 14;
    public List<int>? EmaList { get; set; }
    public int AdxPeriod { get; set; } = 14;
    public string AdxTimeframe { get; set; } = "Current";
}
