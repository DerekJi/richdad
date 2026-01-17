using Trading.Backtest.Services;
using Trading.Data.Configuration;
using Trading.Data.Models;
using Trading.Data.Providers;

namespace Trading.Strategy.Analyzer;

/// <summary>
/// 策略分析器 - 专门分析2024年黄金策略表现不佳的原因
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== 策略性能分析器 ===\n");
        Console.WriteLine("目标：分析2024年黄金策略表现不佳的原因\n");

        var dataDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data"));
        
        // 配置：2024年全年数据
        var config = StrategyConfig.CreateXauDefault();
        config.StrategyName = "PinBar-XAUUSD-v2";
        
        var accountSettings = new AccountSettings
        {
            InitialCapital = 100000,
            Leverage = 30,
            MaxLossPerTradePercent = 0.5,
            MaxDailyLossPercent = 3.0
        };

        Console.WriteLine("正在运行2024年全年回测...");
        var runner = new BacktestRunner();
        var result = await runner.RunAsync(
            config, 
            accountSettings, 
            dataDirectory,
            new DateTime(2024, 3, 1),
            new DateTime(2024, 12, 31)
        );

        // 分析结果
        AnalyzePerformance(result);
        
        // 分析失败交易模式
        AnalyzeLosingTrades(result);
        
        // 分析时间段分布
        AnalyzeTimingPatterns(result);
        
        // 分析市场环境
        AnalyzeMarketConditions(result);

        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }

    static void AnalyzePerformance(BacktestResult result)
    {
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("总体表现分析");
        Console.WriteLine(new string('=', 60));
        
        var trades = result.Trades;
        var winningTrades = trades.Where(t => t.ProfitLoss > 0).ToList();
        var losingTrades = trades.Where(t => t.ProfitLoss < 0).ToList();
        
        Console.WriteLine($"总交易数: {trades.Count}");
        Console.WriteLine($"盈利交易: {winningTrades.Count} ({winningTrades.Count * 100.0 / trades.Count:F2}%)");
        Console.WriteLine($"亏损交易: {losingTrades.Count} ({losingTrades.Count * 100.0 / trades.Count:F2}%)");
        
        var totalProfitLoss = trades.Sum(t => t.ProfitLoss ?? 0);
        Console.WriteLine($"总盈亏: ${totalProfitLoss:F2}");
        
        var returnRate = result.OverallMetrics.TotalReturnRate;
        Console.WriteLine($"收益率: {returnRate:F2}%");
        
        if (winningTrades.Any())
        {
            Console.WriteLine($"\n平均盈利: ${winningTrades.Average(t => t.ProfitLoss ?? 0):F2}");
            Console.WriteLine($"最大盈利: ${winningTrades.Max(t => t.ProfitLoss ?? 0):F2}");
        }
        
        if (losingTrades.Any())
        {
            Console.WriteLine($"平均亏损: ${losingTrades.Average(t => t.ProfitLoss ?? 0):F2}");
            Console.WriteLine($"最大亏损: ${losingTrades.Min(t => t.ProfitLoss ?? 0):F2}");
        }
        
        // 盈亏比分析
        if (winningTrades.Any() && losingTrades.Any())
        {
            var avgWin = winningTrades.Average(t => t.ProfitLoss ?? 0);
            var avgLoss = Math.Abs(losingTrades.Average(t => t.ProfitLoss ?? 0));
            Console.WriteLine($"\n实际盈亏比: {avgWin / avgLoss:F2}");
        }
        
        // 问题识别
        Console.WriteLine("\n【问题识别】");
        if (result.OverallMetrics.WinRate < 30m)
            Console.WriteLine("⚠️ 胜率过低（< 30%），可能策略信号质量不高");
        if (totalProfitLoss < 0)
            Console.WriteLine("⚠️ 总体亏损，策略在2024年市场环境下失效");
        
        var actualRR = winningTrades.Any() && losingTrades.Any() 
            ? winningTrades.Average(t => t.ProfitLoss ?? 0) / Math.Abs(losingTrades.Average(t => t.ProfitLoss ?? 0))
            : 0m;
        if (actualRR < 1.0m)
            Console.WriteLine($"⚠️ 实际盈亏比 {actualRR:F2} < 1.0，平均亏损大于平均盈利");
    }

    static void AnalyzeLosingTrades(BacktestResult result)
    {
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("亏损交易模式分析");
        Console.WriteLine(new string('=', 60));
        
        var losingTrades = result.Trades.Where(t => t.ProfitLoss < 0).ToList();
        
        // 止损原因统计
        var slCount = losingTrades.Count(t => t.CloseReason == TradeCloseReason.StopLoss);
        Console.WriteLine($"\n止损触发: {slCount} / {losingTrades.Count} ({slCount * 100.0 / losingTrades.Count:F2}%)");
        
        // 持仓时间分析
        var tradesWithTime = losingTrades.Where(t => t.CloseTime.HasValue).ToList();
        if (tradesWithTime.Any())
        {
            var avgHoldingTime = tradesWithTime.Average(t => (t.CloseTime!.Value - t.OpenTime).TotalHours);
            Console.WriteLine($"亏损单平均持仓时间: {avgHoldingTime:F2} 小时");
        }
        
        // 方向分析
        var longLosses = losingTrades.Count(t => t.Direction == TradeDirection.Long);
        var shortLosses = losingTrades.Count(t => t.Direction == TradeDirection.Short);
        Console.WriteLine($"\n做多亏损: {longLosses} ({longLosses * 100.0 / losingTrades.Count:F2}%)");
        Console.WriteLine($"做空亏损: {shortLosses} ({shortLosses * 100.0 / losingTrades.Count:F2}%)");
        
        // 按月份统计亏损
        Console.WriteLine("\n月度亏损分布:");
        var monthlyLosses = losingTrades
            .GroupBy(t => t.OpenTime.ToString("yyyy-MM"))
            .OrderBy(g => g.Key);
        
        foreach (var month in monthlyLosses)
        {
            var count = month.Count();
            var totalLoss = month.Sum(t => t.ProfitLoss ?? 0);
            Console.WriteLine($"  {month.Key}: {count}笔, ${totalLoss:F2}");
        }
    }

    static void AnalyzeTimingPatterns(BacktestResult result)
    {
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("时间模式分析");
        Console.WriteLine(new string('=', 60));
        
        var trades = result.Trades;
        
        // UTC小时分布
        Console.WriteLine("\nUTC小时分布:");
        var hourlyStats = trades
            .GroupBy(t => t.OpenTime.Hour)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Hour = g.Key,
                Count = g.Count(),
                WinRate = g.Count(t => t.ProfitLoss > 0) * 100.0 / g.Count(),
                AvgProfit = g.Average(t => t.ProfitLoss ?? 0)
            });
        
        foreach (var hour in hourlyStats)
        {
            Console.WriteLine($"  {hour.Hour:D2}:00 - {hour.Count}笔, 胜率{hour.WinRate:F1}%, 平均盈亏${hour.AvgProfit:F2}");
        }
        
        // 星期分布
        Console.WriteLine("\n星期分布:");
        var weekdayStats = trades
            .GroupBy(t => t.OpenTime.DayOfWeek)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Day = g.Key,
                Count = g.Count(),
                WinRate = g.Count(t => t.ProfitLoss > 0) * 100.0 / g.Count(),
                TotalProfit = g.Sum(t => t.ProfitLoss ?? 0)
            });
        
        foreach (var day in weekdayStats)
        {
            Console.WriteLine($"  {day.Day}: {day.Count}笔, 胜率{day.WinRate:F1}%, 总盈亏${day.TotalProfit:F2}");
        }
    }

    static void AnalyzeMarketConditions(BacktestResult result)
    {
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("市场环境分析");
        Console.WriteLine(new string('=', 60));
        
        Console.WriteLine("\n【2024年黄金市场特征】");
        Console.WriteLine("- 2024年初至3月：强势上涨突破2100");
        Console.WriteLine("- 3-7月：震荡整理，2300-2400区间");
        Console.WriteLine("- 8月后：再次上涨突破2500，波动性加大");
        
        Console.WriteLine("\n【可能的问题】");
        Console.WriteLine("1. Pin Bar在趋势市中表现不佳");
        Console.WriteLine("2. 止损设置在高波动环境下容易被触发");
        Console.WriteLine("3. 策略可能更适合震荡市，不适合单边趋势");
        Console.WriteLine("4. EMA200在快速趋势中滞后性明显");
        
        Console.WriteLine("\n【建议优化方向】");
        Console.WriteLine("□ 添加趋势强度过滤（ADX指标）");
        Console.WriteLine("□ 在趋势市暂停交易或调整参数");
        Console.WriteLine("□ 优化止损策略（移动止损或时间止损）");
        Console.WriteLine("□ 考虑添加波动率过滤（ATR阈值）");
        Console.WriteLine("□ 针对不同市场环境使用自适应参数");
    }
}
