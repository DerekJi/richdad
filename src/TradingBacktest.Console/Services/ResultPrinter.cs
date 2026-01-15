using TradingBacktest.Data.Models;

namespace TradingBacktest.Console.Services;

/// <summary>
/// 回测结果打印服务
/// </summary>
public class ResultPrinter
{
    /// <summary>
    /// 打印完整的回测结果
    /// </summary>
    public void Print(BacktestResult result)
    {
        PrintOverallMetrics(result);
        PrintMonthlyMetrics(result);
        PrintTradeDetails(result);
    }

    /// <summary>
    /// 打印总体统计
    /// </summary>
    private void PrintOverallMetrics(BacktestResult result)
    {
        var metrics = result.OverallMetrics;

        System.Console.WriteLine(new string('=', 60));
        System.Console.WriteLine("总体统计");
        System.Console.WriteLine(new string('=', 60));
        System.Console.WriteLine($"策略名称: {result.Config.StrategyName}");
        System.Console.WriteLine($"回测周期: {result.StartTime:yyyy-MM-dd} 至 {result.EndTime:yyyy-MM-dd}");
        System.Console.WriteLine($"总交易数: {metrics.TotalTrades}");
        System.Console.WriteLine($"盈利交易: {metrics.WinningTrades}");
        System.Console.WriteLine($"亏损交易: {metrics.LosingTrades}");
        System.Console.WriteLine($"胜率: {metrics.WinRate:F2}%");
        System.Console.WriteLine($"总收益: {metrics.TotalProfit:F2} 点");
        System.Console.WriteLine($"总收益率: {metrics.TotalReturnRate:F2}R");
        System.Console.WriteLine($"平均持仓时间: {metrics.AverageHoldingTime}");
        System.Console.WriteLine($"最大连续盈利: {metrics.MaxConsecutiveWins} 单");
        System.Console.WriteLine($"最大连续亏损: {metrics.MaxConsecutiveLosses} 单");
        System.Console.WriteLine($"最大回撤: {metrics.MaxDrawdown:F2} 点 ({metrics.MaxDrawdownTime:yyyy-MM-dd})");
        System.Console.WriteLine($"盈亏比: {metrics.ProfitFactor:F2}");
        System.Console.WriteLine($"平均每月开仓: {metrics.AverageTradesPerMonth:F1} 单");
    }

    /// <summary>
    /// 打印月度统计
    /// </summary>
    private void PrintMonthlyMetrics(BacktestResult result)
    {
        System.Console.WriteLine("\n" + new string('=', 60));
        System.Console.WriteLine("月度统计");
        System.Console.WriteLine(new string('=', 60));
        System.Console.WriteLine($"{"月份",-10} {"交易数",8} {"胜率",10} {"盈亏",12} {"收益率",10}");
        System.Console.WriteLine(new string('-', 60));
        
        foreach (var month in result.MonthlyMetrics.Take(12))
        {
            System.Console.WriteLine($"{month.Period,-10} {month.TradeCount,8} {month.WinRate,9:F1}% {month.ProfitLoss,11:F2} {month.ReturnRate,9:F2}R");
        }
    }

    /// <summary>
    /// 打印交易详情
    /// </summary>
    private void PrintTradeDetails(BacktestResult result)
    {
        System.Console.WriteLine("\n" + new string('=', 60));
        System.Console.WriteLine($"交易详情 (共 {result.Trades.Count} 笔，显示前20笔)");
        System.Console.WriteLine(new string('=', 60));
        System.Console.WriteLine($"{"开仓时间",-17} {"方向",6} {"开仓价",10} {"止损",10} {"止盈",10} {"平仓价",10} {"盈亏",10} {"原因",8}");
        System.Console.WriteLine(new string('-', 60));

        foreach (var trade in result.Trades.Take(20))
        {
            var direction = trade.Direction == TradeDirection.Long ? "多单" : "空单";
            var reason = trade.CloseReason == CloseReason.StopLoss ? "止损" : 
                        trade.CloseReason == CloseReason.TakeProfit ? "止盈" : "手动";
            var pl = trade.ProfitLoss ?? 0;
            var plColor = pl > 0 ? "+" : "";
            
            System.Console.WriteLine(
                $"{trade.OpenTime:yyyy-MM-dd HH:mm} {direction,6} " +
                $"{trade.OpenPrice,10:F2} {trade.StopLoss,10:F2} {trade.TakeProfit,10:F2} " +
                $"{trade.ClosePrice,10:F2} {plColor}{pl,9:F2} {reason,8}");
        }

        if (result.Trades.Count > 20)
        {
            System.Console.WriteLine($"... 还有 {result.Trades.Count - 20} 笔交易未显示");
        }
    }
}
