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
            ClosePosition(openTrade, candles.Last(), Data.Models.TradeCloseReason.Manual, config, accountSettings);
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
        var takeProfit = _strategy.CalculateTakeProfit(entryPrice, stopLoss, direction);
        
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
    private Data.Models.TradeCloseReason? CheckClosePosition(Candle candle, Trade trade)
    {
        if (trade.Direction == TradeDirection.Long)
        {
            // 做多：低点触及止损或高点触及止盈
            if (candle.Low <= trade.StopLoss)
                return Data.Models.TradeCloseReason.StopLoss;
            if (candle.High >= trade.TakeProfit)
                return Data.Models.TradeCloseReason.TakeProfit;
        }
        else
        {
            // 做空：高点触及止损或低点触及止盈
            if (candle.High >= trade.StopLoss)
                return Data.Models.TradeCloseReason.StopLoss;
            if (candle.Low <= trade.TakeProfit)
                return Data.Models.TradeCloseReason.TakeProfit;
        }
        
        return null;
    }

    /// <summary>
    /// 平仓
    /// </summary>
    private void ClosePosition(Trade trade, Candle candle, Data.Models.TradeCloseReason closeReason, StrategyConfig config, AccountSettings accountSettings)
    {
        trade.CloseTime = candle.DateTime;
        trade.CloseReason = closeReason;
        
        // 根据平仓原因确定平仓价格
        trade.ClosePrice = closeReason switch
        {
            Data.Models.TradeCloseReason.StopLoss => trade.StopLoss,
            Data.Models.TradeCloseReason.TakeProfit => trade.TakeProfit,
            Data.Models.TradeCloseReason.Manual => candle.Close,
            _ => candle.Close
        };
        
        // 计算点数差
        decimal priceDiff = trade.Direction == TradeDirection.Long
            ? (trade.ClosePrice!.Value - trade.OpenPrice)
            : (trade.OpenPrice - trade.ClosePrice!.Value);
        
        // 计算USD盈亏 = 点数差 × 合约大小 × 手数
        // 手数根据风险计算: (InitialCapital × MaxLossPerTradePercent%) / (StopLossPips × ContractSize)
        decimal stopLossPips = trade.Direction == TradeDirection.Long 
            ? (trade.OpenPrice - trade.StopLoss) 
            : (trade.StopLoss - trade.OpenPrice);
        
        decimal riskAmount = (decimal)accountSettings.InitialCapital * (decimal)accountSettings.MaxLossPerTradePercent / 100m;
        decimal lotSize = stopLossPips > 0 ? riskAmount / (stopLossPips * config.ContractSize) : 0.01m;
        
        trade.ProfitLoss = Math.Round(priceDiff * config.ContractSize * lotSize, 8);
        
        // 计算收益率 (相对于初始资金的百分比)
        trade.ReturnRate = Math.Round((trade.ProfitLoss ?? 0) / (decimal)accountSettings.InitialCapital * 100m, 8);
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
        CalculateConsecutiveWinsLosses(trades, out var maxWins, out var maxLosses);
        overall.MaxConsecutiveWins = maxWins;
        overall.MaxConsecutiveLosses = maxLosses;
        
        // 计算最大回撤
        CalculateMaxDrawdown(trades, out var maxDrawdown, out var maxDrawdownTime);
        overall.MaxDrawdown = maxDrawdown;
        overall.MaxDrawdownTime = maxDrawdownTime;
        
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
    private void CalculateConsecutiveWinsLosses(List<Trade> trades, out int maxWins, out int maxLosses)
    {
        maxWins = 0;
        maxLosses = 0;
        int currentWins = 0;
        int currentLosses = 0;
        
        foreach (var trade in trades)
        {
            if (trade.IsWinning == true)
            {
                currentWins++;
                currentLosses = 0;
                maxWins = Math.Max(maxWins, currentWins);
            }
            else if (trade.IsWinning == false)
            {
                currentLosses++;
                currentWins = 0;
                maxLosses = Math.Max(maxLosses, currentLosses);
            }
        }
    }

    /// <summary>
    /// 计算最大回撤
    /// </summary>
    private void CalculateMaxDrawdown(List<Trade> trades, out decimal maxDrawdown, out DateTime? maxDrawdownTime)
    {
        maxDrawdown = 0;
        maxDrawdownTime = null;
        
        decimal peak = 0;
        decimal cumulative = 0;
        
        foreach (var trade in trades)
        {
            cumulative += trade.ProfitLoss ?? 0;
            
            if (cumulative > peak)
            {
                peak = cumulative;
            }
            
            var drawdown = peak - cumulative;
            if (drawdown > maxDrawdown)
            {
                maxDrawdown = drawdown;
                maxDrawdownTime = trade.CloseTime;
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
            
            CalculateConsecutiveWinsLosses(periodTrades, out var maxWins, out var maxLosses);
            
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

    private enum PeriodType
    {
        Week,
        Month,
        Year
    }
}
