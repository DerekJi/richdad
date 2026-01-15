using TradingBacktest.Core.Backtest;
using TradingBacktest.Core.Strategies;
using TradingBacktest.Data.Interfaces;
using TradingBacktest.Data.Models;
using TradingBacktest.Data.Providers;

namespace TradingBacktest.Console.Services;

/// <summary>
/// 回测运行服务
/// </summary>
public class BacktestRunner
{
    private readonly string _dataDirectory;

    public BacktestRunner(string dataDirectory)
    {
        _dataDirectory = dataDirectory;
    }

    /// <summary>
    /// 执行回测
    /// </summary>
    public async Task<BacktestResult> RunAsync(StrategyConfig config)
    {
        System.Console.WriteLine($"数据目录: {_dataDirectory}\n");

        // 创建数据提供者
        IMarketDataProvider dataProvider = new CsvDataProvider(_dataDirectory);

        // 加载数据
        System.Console.WriteLine($"正在加载 {config.Symbol} 的历史数据...");
        var candles = await dataProvider.GetCandlesAsync(config.Symbol);
        System.Console.WriteLine($"加载完成，共 {candles.Count} 根K线");
        System.Console.WriteLine($"数据范围: {candles.First().DateTime:yyyy-MM-dd HH:mm} 至 {candles.Last().DateTime:yyyy-MM-dd HH:mm}\n");

        // 创建策略
        var strategy = new PinBarStrategy(config);

        // 创建回测引擎
        var backtestEngine = new BacktestEngine(strategy);

        // 执行回测
        System.Console.WriteLine("开始执行回测...\n");
        return backtestEngine.RunBacktest(candles, config);
    }
}
