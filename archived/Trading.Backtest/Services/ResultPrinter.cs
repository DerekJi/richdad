using Trading.Backtest.Data.Models;
using Trading.Models;

namespace Trading.Backtest.Services;

/// <summary>
/// 回测结果打印服务
/// </summary>
public class ResultPrinter
{
    /// <summary>
    /// 打印完整的回测结果
    /// </summary>
    public void Print(BacktestResult result, decimal initialCapital, decimal leverage)
    {
        PrintOverallMetrics(result, initialCapital, leverage);
        PrintYearlyMetrics(result);
        PrintMonthlyMetrics(result);
        PrintWeeklyMetrics(result);
        PrintTimeSlotAnalysis(result);
        PrintEquityCurve(result);
        PrintTradeDetails(result);
    }

    /// <summary>
    /// 打印总体统计
    /// </summary>
    private void PrintOverallMetrics(BacktestResult result, decimal initialCapital, decimal leverage)
    {
        var metrics = result.OverallMetrics;

        Console.WriteLine(new string('=', 80));
        Console.WriteLine("总体统计");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine($"策略名称: {result.Config.StrategyName}");
        Console.WriteLine($"交易品种: {result.Config.Symbol}");
        Console.WriteLine($"回测周期: {result.StartTime:yyyy-MM-dd} 至 {result.EndTime:yyyy-MM-dd}");
        Console.WriteLine($"初始资金: {initialCapital:N2} USD (杠杆: {leverage}x)");
        Console.WriteLine(new string('-', 80));
        Console.WriteLine($"总交易数: {metrics.TotalTrades}");
        Console.WriteLine($"盈利交易: {metrics.WinningTrades} ({metrics.WinRate:F2}%)");
        Console.WriteLine($"亏损交易: {metrics.LosingTrades}");
        Console.WriteLine($"总收益: {metrics.TotalProfit:N2} USD");
        Console.WriteLine($"总收益率: {metrics.TotalReturnRate:F2}%");
        Console.WriteLine($"最终资金: {(initialCapital + metrics.TotalProfit):N2} USD");
        Console.WriteLine($"盈亏比: {metrics.ProfitFactor:F2}");
        Console.WriteLine($"平均持仓时间: {FormatTimeSpan(metrics.AverageHoldingTime)}");
        Console.WriteLine($"最大连续盈利: {metrics.MaxConsecutiveWins} 单");
        Console.WriteLine($"最大连续亏损: {metrics.MaxConsecutiveLosses} 单");
        Console.WriteLine($"最大回撤: {metrics.MaxDrawdown:N2} USD ({(metrics.MaxDrawdown / initialCapital * 100):F2}%) ({(metrics.MaxDrawdownEndTime.HasValue ? metrics.MaxDrawdownEndTime.Value.ToString("yyyy-MM-dd") : "N/A")}");
        Console.WriteLine($"平均每月开仓: {metrics.AverageTradesPerMonth:F1} 单");
    }

    /// <summary>
    /// 打印年度统计
    /// </summary>
    private void PrintYearlyMetrics(BacktestResult result)
    {
        if (result.YearlyMetrics.Count == 0) return;

        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("年度统计");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine($"{"年份",-8} {"交易数",8} {"胜率",10} {"盈亏(USD)",15} {"收益率",10} {"持仓时间",15} {"连胜",8} {"连亏",8}");
        Console.WriteLine(new string('-', 80));

        foreach (var year in result.YearlyMetrics)
        {
            Console.WriteLine(
                $"{year.Period,-8} " +
                $"{year.TradeCount,8} " +
                $"{year.WinRate,9:F1}% " +
                $"{year.ProfitLoss,14:N2} " +
                $"{year.ReturnRate,9:F2}% " +
                $"{FormatTimeSpan(year.AverageHoldingTime),15} " +
                $"{year.MaxConsecutiveWins,8} " +
                $"{year.MaxConsecutiveLosses,8}");
        }
    }

    /// <summary>
    /// 打印月度统计
    /// </summary>
    private void PrintMonthlyMetrics(BacktestResult result)
    {
        if (result.MonthlyMetrics.Count == 0) return;

        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("月度统计 (最近12个月)");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine($"{"月份",-10} {"交易数",8} {"胜率",10} {"盈亏(USD)",15} {"收益率",10} {"持仓时间",15} {"连胜",8} {"连亏",8}");
        Console.WriteLine(new string('-', 80));

        foreach (var month in result.MonthlyMetrics.TakeLast(12))
        {
            Console.WriteLine(
                $"{month.Period,-10} " +
                $"{month.TradeCount,8} " +
                $"{month.WinRate,9:F1}% " +
                $"{month.ProfitLoss,14:N2} " +
                $"{month.ReturnRate,9:F2}% " +
                $"{FormatTimeSpan(month.AverageHoldingTime),15} " +
                $"{month.MaxConsecutiveWins,8} " +
                $"{month.MaxConsecutiveLosses,8}");
        }
    }

    /// <summary>
    /// 打印周度统计
    /// </summary>
    private void PrintWeeklyMetrics(BacktestResult result)
    {
        if (result.WeeklyMetrics.Count == 0) return;

        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("周度统计 (最近12周)");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine($"{"周",-12} {"交易数",8} {"胜率",10} {"盈亏(USD)",15} {"收益率",10} {"持仓时间",15} {"连胜",8} {"连亏",8}");
        Console.WriteLine(new string('-', 80));

        foreach (var week in result.WeeklyMetrics.TakeLast(12))
        {
            Console.WriteLine(
                $"{week.Period,-12} " +
                $"{week.TradeCount,8} " +
                $"{week.WinRate,9:F1}% " +
                $"{week.ProfitLoss,14:N2} " +
                $"{week.ReturnRate,9:F2}% " +
                $"{FormatTimeSpan(week.AverageHoldingTime),15} " +
                $"{week.MaxConsecutiveWins,8} " +
                $"{week.MaxConsecutiveLosses,8}");
        }
    }

    /// <summary>
    /// 打印时间段盈亏分析
    /// </summary>
    private void PrintTimeSlotAnalysis(BacktestResult result)
    {
        Console.WriteLine("\n" + TimeSlotAnalyzer.GenerateTimeSlotReportText(result.Trades));
    }

    /// <summary>
    /// 打印收益曲线摘要
    /// </summary>
    private void PrintEquityCurve(BacktestResult result)
    {
        if (result.EquityCurve.Count == 0) return;

        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("收益曲线摘要 (最近10个数据点)");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine($"{"时间",-20} {"累计收益(USD)",20} {"累计收益率",15}");
        Console.WriteLine(new string('-', 80));

        foreach (var point in result.EquityCurve.TakeLast(10))
        {
            Console.WriteLine(
                $"{point.Time:yyyy-MM-dd HH:mm:ss,-20} " +
                $"{point.CumulativeProfit,19:N2} " +
                $"{point.CumulativeReturnRate,14:F2}%");
        }

        Console.WriteLine($"\n注: 完整收益曲线共 {result.EquityCurve.Count} 个数据点已保存至数据库");
    }

    /// <summary>
    /// 打印交易详情
    /// </summary>
    private void PrintTradeDetails(BacktestResult result)
    {
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine($"交易详情 (共 {result.Trades.Count} 笔，显示前20笔)");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine($"{"开仓时间",-17} {"方向",6} {"开仓价",10} {"止损",10} {"止盈",10} {"平仓价",10} {"盈亏(USD)",12} {"收益率",10} {"原因",8}");
        Console.WriteLine(new string('-', 80));

        foreach (var trade in result.Trades.Take(20))
        {
            var direction = trade.Direction == TradeDirection.Long ? "多单" : "空单";
            var reason = trade.CloseReason == TradeCloseReason.StopLoss ? "止损" :
                        trade.CloseReason == TradeCloseReason.TakeProfit ? "止盈" : "手动";
            var pl = trade.ProfitLoss ?? 0;
            var plPrefix = pl > 0 ? "+" : "";
            var returnRate = trade.ReturnRate ?? 0;
            var returnPrefix = returnRate > 0 ? "+" : "";

            Console.WriteLine(
                $"{trade.OpenTime:yyyy-MM-dd HH:mm} {direction,6} " +
                $"{trade.OpenPrice,10:F2} {trade.StopLoss,10:F2} {trade.TakeProfit,10:F2} " +
                $"{trade.ClosePrice,10:F2} {plPrefix}{pl,11:N2} {returnPrefix}{returnRate,9:F2}% {reason,8}");
        }

        if (result.Trades.Count > 20)
        {
            Console.WriteLine($"... 还有 {result.Trades.Count - 20} 笔交易未显示");
        }
    }

    /// <summary>
    /// 格式化时间跨度
    /// </summary>
    private string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalDays >= 1)
            return $"{timeSpan.TotalDays:F1}天";
        else if (timeSpan.TotalHours >= 1)
            return $"{timeSpan.TotalHours:F1}小时";
        else
            return $"{timeSpan.TotalMinutes:F0}分钟";
    }
}
