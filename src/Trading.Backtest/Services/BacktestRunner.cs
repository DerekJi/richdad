using Trading.Backtest.Engine;
using Trading.Core.Strategies;
using Trading.Data.Interfaces;
using Trading.Data.Models;
using Trading.Data.Providers;
using Trading.Data.Configuration;

namespace Trading.Backtest.Services;

/// <summary>
/// 回测运行服务
/// 职责：加载数据并执行回测
/// </summary>
public class BacktestRunner
{
    public List<Candle> LoadedCandles { get; private set; } = new();

    /// <summary>
    /// 加载CSV数据（供Web API使用）
    /// </summary>
    public List<Candle> LoadCandlesFromCsv(string dataDirectory, string symbol, string csvFilter)
    {
        var dataProvider = new CsvDataProvider(dataDirectory);
        var candles = dataProvider.GetCandlesAsync(symbol, csvFilter).GetAwaiter().GetResult();
        return candles;
    }

    /// <summary>
    /// 执行回测
    /// </summary>
    public async Task<BacktestResult> RunAsync(StrategyConfig config, AccountSettings accountSettings, string dataDirectory, DateTime? startDate = null, DateTime? endDate = null)
    {
        Console.WriteLine($"数据目录: {dataDirectory}\n");

        // 创建数据提供者
        IMarketDataProvider dataProvider = new CsvDataProvider(dataDirectory);

        // 加载数据
        Console.WriteLine($"正在加载 {config.Symbol} 的历史数据...");
        var candles = await dataProvider.GetCandlesAsync(config.Symbol, config.CsvFilter);

        // 根据日期范围过滤数据
        if (startDate.HasValue || endDate.HasValue)
        {
            var originalCount = candles.Count;
            if (startDate.HasValue)
            {
                candles = candles.Where(c => c.DateTime >= startDate.Value).ToList();
                Console.WriteLine($"应用开始日期过滤: {startDate.Value:yyyy-MM-dd}");
            }
            if (endDate.HasValue)
            {
                candles = candles.Where(c => c.DateTime <= endDate.Value.AddDays(1).AddSeconds(-1)).ToList();
                Console.WriteLine($"应用结束日期过滤: {endDate.Value:yyyy-MM-dd}");
            }
            Console.WriteLine($"过滤后剩余 {candles.Count} 根K线 (原始: {originalCount})");
        }

        LoadedCandles = candles; // 保存供诊断使用
        Console.WriteLine($"加载完成，共 {candles.Count} 根K线");
        Console.WriteLine($"数据范围: {candles.First().DateTime:yyyy-MM-dd HH:mm} 至 {candles.Last().DateTime:yyyy-MM-dd HH:mm}\n");

        // 创建策略
        var strategy = new PinBarStrategy(config);

        // 创建回测引擎
        var backtestEngine = new BacktestEngine(strategy);

        // 执行回测
        Console.WriteLine("开始执行回测...\n");
        var result = backtestEngine.RunBacktest(candles, config, accountSettings);
        Console.WriteLine($"回测执行完毕。完成 {result.Trades.Count} 笔交易\n");

        return result;
    }
}
