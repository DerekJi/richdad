using System.Globalization;
using Trading.Core.Indicators;
using Trading.Core.Strategies;
using Trading.Data.Models;
using Trading.Data.Configuration;

namespace Trading.Backtest.Engine;

/// <summary>
/// 回测引擎
/// </summary>
public class BacktestEngine
{
    private readonly ITradingStrategy _strategy;
    private readonly IndicatorCalculator _indicatorCalculator;
    private int _consecutiveLosses = 0;
    private DateTime? _pauseUntilDate = null;

    // 动态风险管理
    private int _riskReductionLevel = 0; // 当前减半级别 (0=正常, 1=减半1次, 2=减半2次...)
    private decimal _currentEquity = 0; // 当前净值
    private decimal _initialEquity = 0; // 初始净值
    private List<decimal> _reductionThresholds = new List<decimal>(); // 每个级别的恢复阈值
    private int _consecutiveLossesAtCurrentLevel = 0; // 当前级别的连续亏损数

    public BacktestEngine(ITradingStrategy strategy)
    {
        _strategy = strategy;
        _indicatorCalculator = new IndicatorCalculator();
    }

    /// <summary>
    /// 执行回测
    /// </summary>
    public BacktestResult RunBacktest(List<Candle> candles, StrategyConfig config, AccountSettings accountSettings)
    {
        // 计算技术指标
        _indicatorCalculator.CalculateIndicators(candles, config);

        // 初始化动态风险管理
        _initialEquity = (decimal)accountSettings.InitialCapital;
        _currentEquity = _initialEquity;
        _riskReductionLevel = 0;
        _reductionThresholds.Clear();
        _consecutiveLossesAtCurrentLevel = 0;

        var result = new BacktestResult
        {
            Config = config,
            StartTime = candles.First().DateTime,
            EndTime = candles.Last().DateTime
        };

        Trade? openTrade = null;

        // 遍历K线执行回测
        for (int i = 1; i < candles.Count; i++)
        {
            var current = candles[i];
            var previous = candles[i - 1];

            // 检查是否需要平仓
            if (openTrade != null)
            {
                var closeReason = CheckClosePosition(current, openTrade);
                if (closeReason.HasValue)
                {
                    ClosePosition(openTrade, current, closeReason.Value, config, accountSettings);
                    result.Trades.Add(openTrade);
                    openTrade = null;
                }
            }

            // 检查是否可以开仓
            if (openTrade == null)
            {
                // 检查是否在暂停期内
                if (IsInPausePeriod(current.DateTime))
                {
                    continue;
                }

                // 检查风险敦口是否足够 (低于10 USD停止交易)
                decimal currentRiskPercent = GetCurrentRiskPercent(accountSettings.MaxLossPerTradePercent);
                decimal currentRiskAmount = _currentEquity * currentRiskPercent / 100m;
                if (currentRiskAmount < 10m)
                {
                    continue; // 风险敦口太小，跳过交易
                }

                // 检查做多
                if (_strategy.CanOpenLong(current, previous, openTrade != null))
                {
                    openTrade = OpenPosition(current, previous, TradeDirection.Long);
                }
                // 检查做空
                else if (_strategy.CanOpenShort(current, previous, openTrade != null))
                {
                    openTrade = OpenPosition(current, previous, TradeDirection.Short);
                }
            }
        }

        // 如果还有未平仓的交易，在最后一根K线收盘价平仓
        if (openTrade != null)
        {
            ClosePosition(openTrade, candles.Last(), TradeCloseReason.Manual, config, accountSettings);
            result.Trades.Add(openTrade);
        }

        // 计算统计指标
        CalculateMetrics(result, accountSettings);

        return result;
    }

    /// <summary>
    /// 开仓
    /// </summary>
    private Trade OpenPosition(Candle current, Candle pinbar, TradeDirection direction)
    {
        var stopLoss = _strategy.CalculateStopLoss(pinbar, direction);
        var entryPrice = current.Close; // 使用当前K线收盘价开仓
        var takeProfit = _strategy.CalculateTakeProfit(entryPrice, stopLoss, direction, pinbar);

        return new Trade
        {
            Direction = direction,
            OpenTime = current.DateTime,
            OpenPrice = entryPrice,
            StopLoss = stopLoss,
            TakeProfit = takeProfit
        };
    }

    /// <summary>
    /// 检查是否需要平仓
    /// </summary>
    private TradeCloseReason? CheckClosePosition(Candle candle, Trade trade)
    {
        if (trade.Direction == TradeDirection.Long)
        {
            // 做多：低点触及止损或高点触及止盈
            if (candle.Low <= trade.StopLoss)
                return TradeCloseReason.StopLoss;
            if (candle.High >= trade.TakeProfit)
                return TradeCloseReason.TakeProfit;
        }
        else
        {
            // 做空：高点触及止损或低点触及止盈
            if (candle.High >= trade.StopLoss)
                return TradeCloseReason.StopLoss;
            if (candle.Low <= trade.TakeProfit)
                return TradeCloseReason.TakeProfit;
        }

        return null;
    }

    /// <summary>
    /// 平仓
    /// </summary>
    private void ClosePosition(Trade trade, Candle candle, TradeCloseReason closeReason, StrategyConfig config, AccountSettings accountSettings)
    {
        trade.CloseTime = candle.DateTime;
        trade.CloseReason = closeReason;

        // 根据平仓原因确定平仓价格
        trade.ClosePrice = closeReason switch
        {
            TradeCloseReason.StopLoss => trade.StopLoss,
            TradeCloseReason.TakeProfit => trade.TakeProfit,
            TradeCloseReason.Manual => candle.Close,
            _ => candle.Close
        };

        // 计算点数差
        decimal priceDiff = trade.Direction == TradeDirection.Long
            ? (trade.ClosePrice!.Value - trade.OpenPrice)
            : (trade.OpenPrice - trade.ClosePrice!.Value);

        // 计算USD盈亏 = 点数差 × 合约大小 × 手数
        // 手数根据动态风险计算
        decimal stopLossPips = trade.Direction == TradeDirection.Long
            ? (trade.OpenPrice - trade.StopLoss)
            : (trade.StopLoss - trade.OpenPrice);

        // 获取当前动态风险敦口
        decimal currentRiskPercent = GetCurrentRiskPercent(accountSettings.MaxLossPerTradePercent);
        decimal riskAmount = _currentEquity * currentRiskPercent / 100m;

        // 确保风险金额不低于10 USD
        if (riskAmount < 10m)
        {
            riskAmount = 10m;
        }

        decimal lotSize = stopLossPips > 0 ? riskAmount / (stopLossPips * config.ContractSize) : 0.01m;

        trade.ProfitLoss = Math.Round(priceDiff * config.ContractSize * lotSize, 8);

        // 计算收益率 (相对于初始资金的百分比)
        trade.ReturnRate = Math.Round((trade.ProfitLoss ?? 0) / (decimal)accountSettings.InitialCapital * 100m, 8);

        // 更新净值
        _currentEquity += trade.ProfitLoss ?? 0;

        // 更新动态风险管理状态
        UpdateDynamicRiskManagement(trade, config);
    }

    /// <summary>
    /// 计算统计指标
    /// </summary>
    private void CalculateMetrics(BacktestResult result, AccountSettings accountSettings)
    {
        var trades = result.Trades;
        if (trades.Count == 0) return;

        // 总体指标
        var overall = result.OverallMetrics;
        overall.TotalTrades = trades.Count;
        overall.WinningTrades = trades.Count(t => t.IsWinning == true);
        overall.LosingTrades = trades.Count(t => t.IsWinning == false);
        overall.TotalProfit = trades.Sum(t => t.ProfitLoss ?? 0);
        overall.TotalReturnRate = trades.Sum(t => t.ReturnRate ?? 0);
        overall.AverageHoldingTime = TimeSpan.FromTicks((long)trades.Average(t => t.HoldingDuration?.Ticks ?? 0));

        // 计算最大连续盈亏
        CalculateConsecutiveWinsLosses(trades, out var maxWins, out var maxWinsStart, out var maxWinsEnd,
            out var maxLosses, out var maxLossesStart, out var maxLossesEnd);
        overall.MaxConsecutiveWins = maxWins;
        overall.MaxConsecutiveWinsStartTime = maxWinsStart;
        overall.MaxConsecutiveWinsEndTime = maxWinsEnd;
        overall.MaxConsecutiveLosses = maxLosses;
        overall.MaxConsecutiveLossesStartTime = maxLossesStart;
        overall.MaxConsecutiveLossesEndTime = maxLossesEnd;

        // 计算最大回撤
        CalculateMaxDrawdown(trades, out var maxDrawdown, out var maxDrawdownStart, out var maxDrawdownEnd);
        overall.MaxDrawdown = maxDrawdown;
        overall.MaxDrawdownStartTime = maxDrawdownStart;
        overall.MaxDrawdownEndTime = maxDrawdownEnd;

        // 计算盈亏比
        var totalWin = trades.Where(t => t.IsWinning == true).Sum(t => t.ProfitLoss ?? 0);
        var totalLoss = Math.Abs(trades.Where(t => t.IsWinning == false).Sum(t => t.ProfitLoss ?? 0));
        overall.ProfitFactor = totalLoss > 0 ? totalWin / totalLoss : 0;

        // 计算平均每月开仓单数
        var months = (result.EndTime.Year - result.StartTime.Year) * 12 + result.EndTime.Month - result.StartTime.Month + 1;
        overall.AverageTradesPerMonth = months > 0 ? (decimal)trades.Count / months : 0;

        // 周期指标
        result.WeeklyMetrics = CalculatePeriodMetrics(trades, PeriodType.Week);
        result.MonthlyMetrics = CalculatePeriodMetrics(trades, PeriodType.Month);
        result.YearlyMetrics = CalculatePeriodMetrics(trades, PeriodType.Year);

        // 收益曲线
        result.EquityCurve = CalculateEquityCurve(trades, accountSettings);
    }

    /// <summary>
    /// 计算最大连续盈亏
    /// </summary>
    private void CalculateConsecutiveWinsLosses(List<Trade> trades,
        out int maxWins, out DateTime? maxWinsStart, out DateTime? maxWinsEnd,
        out int maxLosses, out DateTime? maxLossesStart, out DateTime? maxLossesEnd)
    {
        maxWins = 0;
        maxWinsStart = null;
        maxWinsEnd = null;
        maxLosses = 0;
        maxLossesStart = null;
        maxLossesEnd = null;

        int currentWins = 0;
        int currentLosses = 0;
        DateTime? currentWinsStart = null;
        DateTime? currentLossesStart = null;

        foreach (var trade in trades)
        {
            if (trade.IsWinning == true)
            {
                if (currentWins == 0)
                {
                    currentWinsStart = trade.OpenTime;
                }
                currentWins++;
                currentLosses = 0;
                currentLossesStart = null;

                if (currentWins > maxWins)
                {
                    maxWins = currentWins;
                    maxWinsStart = currentWinsStart;
                    maxWinsEnd = trade.CloseTime;
                }
            }
            else if (trade.IsWinning == false)
            {
                if (currentLosses == 0)
                {
                    currentLossesStart = trade.OpenTime;
                }
                currentLosses++;
                currentWins = 0;
                currentWinsStart = null;

                if (currentLosses > maxLosses)
                {
                    maxLosses = currentLosses;
                    maxLossesStart = currentLossesStart;
                    maxLossesEnd = trade.CloseTime;
                }
            }
        }
    }

    /// <summary>
    /// 计算最大回撤
    /// </summary>
    private void CalculateMaxDrawdown(List<Trade> trades, out decimal maxDrawdown,
        out DateTime? maxDrawdownStart, out DateTime? maxDrawdownEnd)
    {
        maxDrawdown = 0;
        maxDrawdownStart = null;
        maxDrawdownEnd = null;

        decimal peak = 0;
        decimal cumulative = 0;
        DateTime? peakTime = null;

        foreach (var trade in trades)
        {
            cumulative += trade.ProfitLoss ?? 0;

            if (cumulative > peak)
            {
                peak = cumulative;
                peakTime = trade.CloseTime;
            }

            var drawdown = peak - cumulative;
            if (drawdown > maxDrawdown)
            {
                maxDrawdown = drawdown;
                maxDrawdownStart = peakTime;
                maxDrawdownEnd = trade.CloseTime;
            }
        }
    }

    /// <summary>
    /// 计算周期指标
    /// </summary>
    private List<PeriodMetrics> CalculatePeriodMetrics(List<Trade> trades, PeriodType periodType)
    {
        var grouped = trades.GroupBy(t => GetPeriodKey(t.OpenTime, periodType));
        var metrics = new List<PeriodMetrics>();

        foreach (var group in grouped.OrderBy(g => g.Key))
        {
            var periodTrades = group.ToList();
            var winningTrades = periodTrades.Count(t => t.IsWinning == true);

            CalculateConsecutiveWinsLosses(periodTrades, out var maxWins, out _, out _,
                out var maxLosses, out _, out _);

            metrics.Add(new PeriodMetrics
            {
                Period = group.Key,
                StartDate = periodTrades.Min(t => t.OpenTime),
                EndDate = periodTrades.Max(t => t.CloseTime ?? t.OpenTime),
                TradeCount = periodTrades.Count,
                WinningTrades = winningTrades,
                ProfitLoss = periodTrades.Sum(t => t.ProfitLoss ?? 0),
                ReturnRate = periodTrades.Sum(t => t.ReturnRate ?? 0),
                AverageHoldingTime = TimeSpan.FromTicks((long)periodTrades.Average(t => t.HoldingDuration?.Ticks ?? 0)),
                MaxConsecutiveWins = maxWins,
                MaxConsecutiveLosses = maxLosses
            });
        }

        return metrics;
    }

    /// <summary>
    /// 计算收益曲线
    /// </summary>
    private List<EquityPoint> CalculateEquityCurve(List<Trade> trades, AccountSettings accountSettings)
    {
        var curve = new List<EquityPoint>();
        decimal cumulativeProfit = 0;

        foreach (var trade in trades.OrderBy(t => t.CloseTime))
        {
            cumulativeProfit += trade.ProfitLoss ?? 0;

            curve.Add(new EquityPoint
            {
                Time = trade.CloseTime!.Value,
                CumulativeProfit = cumulativeProfit,
                CumulativeReturnRate = cumulativeProfit / (decimal)accountSettings.InitialCapital * 100m,
                TradeId = trade.Id
            });
        }

        return curve;
    }

    /// <summary>
    /// 获取周期标识
    /// </summary>
    private string GetPeriodKey(DateTime dateTime, PeriodType periodType)
    {
        return periodType switch
        {
            PeriodType.Week => $"{dateTime.Year}-W{ISOWeek.GetWeekOfYear(dateTime):00}",
            PeriodType.Month => $"{dateTime.Year}-{dateTime.Month:00}",
            PeriodType.Year => $"{dateTime.Year}",
            _ => dateTime.ToString("yyyy-MM-dd")
        };
    }

    /// <summary>
    /// 检查是否在暂停期内
    /// </summary>
    private bool IsInPausePeriod(DateTime currentTime)
    {
        if (_pauseUntilDate.HasValue && currentTime.Date <= _pauseUntilDate.Value.Date)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 获取当前动态风险百分比
    /// </summary>
    private decimal GetCurrentRiskPercent(double baseRiskPercent)
    {
        // 根据减半级别计算当前风险
        decimal risk = (decimal)baseRiskPercent;
        for (int i = 0; i < _riskReductionLevel; i++)
        {
            risk = risk / 2m;
        }
        return risk;
    }

    /// <summary>
    /// 更新动态风险管理状态
    /// </summary>
    private void UpdateDynamicRiskManagement(Trade trade, StrategyConfig config)
    {
        // 如果未启用连续亏损规则，使用原有逻辑
        if (config.MaxConsecutiveLosses <= 0)
        {
            UpdateConsecutiveLossesLegacy(trade, config);
            return;
        }

        if (trade.IsWinning == false) // 亏损
        {
            _consecutiveLossesAtCurrentLevel++;

            // 如果达到连续亏损上限，减半风险敦口
            if (_consecutiveLossesAtCurrentLevel >= config.MaxConsecutiveLosses)
            {
                // 记录当前净值作为恢复阈值
                _reductionThresholds.Add(_currentEquity);

                // 进入下一级别
                _riskReductionLevel++;
                _consecutiveLossesAtCurrentLevel = 0;

                Console.WriteLine($"\u8fde续亏损{config.MaxConsecutiveLosses}次，风险敦口减半到第{_riskReductionLevel}级，当前净值: ${_currentEquity:F2}");
            }
        }
        else if (trade.IsWinning == true) // 盈利
        {
            _consecutiveLossesAtCurrentLevel = 0; // 重置当前级别的连续亏损计数

            // 检查是否可以恢复上一级别的风险
            if (_riskReductionLevel > 0 && _reductionThresholds.Count > 0)
            {
                // 检查是否超过最近一次减半的阈值
                decimal lastThreshold = _reductionThresholds[_reductionThresholds.Count - 1];
                if (_currentEquity >= lastThreshold)
                {
                    // 恢复上一级别
                    _riskReductionLevel--;
                    _reductionThresholds.RemoveAt(_reductionThresholds.Count - 1);

                    Console.WriteLine($"\u51c0值恢复到 ${_currentEquity:F2}，风险敦口恢复到第{_riskReductionLevel}级");
                }
            }
        }
    }

    /// <summary>
    /// 原有的连续亏损管理（向后兼容）
    /// </summary>
    private void UpdateConsecutiveLossesLegacy(Trade trade, StrategyConfig config)
    {
        // 如果未启用连续亏损规则，直接返回
        if (config.MaxConsecutiveLosses <= 0)
        {
            return;
        }

        if (trade.IsWinning == false) // 亏损
        {
            _consecutiveLosses++;

            // 如果达到连续亏损上限，设置暂停期
            if (_consecutiveLosses >= config.MaxConsecutiveLosses)
            {
                _pauseUntilDate = trade.CloseTime!.Value.Date.AddDays(config.PauseDaysAfterLosses);
                _consecutiveLosses = 0; // 重置计数器
            }
        }
        else if (trade.IsWinning == true) // 盈利
        {
            _consecutiveLosses = 0; // 重置连续亏损计数
            _pauseUntilDate = null; // 清除暂停期
        }
    }

    private enum PeriodType
    {
        Week,
        Month,
        Year
    }
}
