using Trading.Backtest.ParameterOptimizer.Models;
using Trading.Backtest.ParameterOptimizer.Services;
using Trading.Data.Interfaces;
using Trading.Data.Providers;

namespace Trading.Backtest.ParameterOptimizer.Commands;

/// <summary>
/// 参数优化命令
/// </summary>
public class OptimizerCommand
{
    private static string? CsvFilter = "";
    private static DateTime? StartTime { get; set; } = new DateTime(2022, 5, 1);
    private static DateTime? EndTime { get; set; } = null; // new DateTime(2023, 5, 1);

    public static async Task ExecuteAsync()
    {
        Console.WriteLine("=== Pin Bar Strategy Parameter Optimizer ===\n");

        // 预加载CSV数据（只加载一次）
        Console.WriteLine("正在加载历史数据...");
        var dataDirectory = Path.GetFullPath(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "data"));
        Console.WriteLine($"数据目录: {dataDirectory}");

        IMarketDataProvider dataProvider = new CsvDataProvider(dataDirectory);
        var candles = await dataProvider.GetCandlesAsync("XAUUSD", CsvFilter, StartTime, EndTime);

        Console.WriteLine($"✓ 数据加载完成: {candles.Count} 根K线");
        Console.WriteLine($"  数据范围: {candles.First().DateTime:yyyy-MM-dd} 至 {candles.Last().DateTime:yyyy-MM-dd}\n");

        // 定义参数搜索空间
        var parameterSpace = new ParameterSpace
        {
            // Pin Bar形态参数
            MaxBodyPercentage = [25], // BEST of ParameterRangeHelper.SetRange(25, 50, 5),
            MinLongerWickPercentage = [45], // BEST of ParameterRangeHelper.SetRange(40, 70, 5),
            MaxShorterWickPercentage =[35], // BEST of ParameterRangeHelper.SetRange(10, 40, 5),

            // EMA相关参数
            NearEmaThreshold = [0.6m], // BEST of ParameterRangeHelper.SetRange(0.5m, 0.8m, 0.1m), // ParameterRangeHelper.SetRange(0.8m, 3.0m, 0.3m),

            // 止损止盈参数
            StopLossAtrRatio = [1m], // BEST of ParameterRangeHelper.SetRange(1.0m, 2.0m, 0.5m),
            RiskRewardRatio = [2.5m], // BEST of ParameterRangeHelper.SetRange(1.5m, 2.5m, 0.5m),

            // 风控参数
            MaxLossPerTradePercent = [1m], // BEST of ParameterRangeHelper.SetRange(0.5m, 1.0m, 0.1m)
        };

        // 创建服务
        var executor = new BacktestExecutor(candles);
        var resultsManager = new ResultsManager();
        var optimizer = new Services.ParameterOptimizer(executor, resultsManager);

        // 执行优化
        await optimizer.OptimizeAsync(parameterSpace);
    }
}
