using System.Text.Json;
using Trading.Backtest.Engine;
using Trading.Backtest.Services;
using Trading.Core.Strategies;
using Trading.Data.Configuration;
using Trading.Data.Interfaces;
using Trading.Data.Models;
using Trading.Data.Providers;

namespace Trading.Backtest.ParameterOptimizer;

public class Program
{
    private static readonly List<OptimizationResult> _results = new();
    private static List<Candle> _candles = new();
    private const string ResultsDirectory = "results";

    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Pin Bar Strategy Parameter Optimizer ===\n");

        // 创建结果目录
        Directory.CreateDirectory(ResultsDirectory);

        // 预加载CSV数据（只加载一次）
        Console.WriteLine("正在加载历史数据...");
        var dataDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "data"));
        Console.WriteLine($"数据目录: {dataDirectory}");
        IMarketDataProvider dataProvider = new CsvDataProvider(dataDirectory);
        _candles = await dataProvider.GetCandlesAsync("XAUUSD", "", startTime: new DateTime(2023, 5, 1));
        Console.WriteLine($"✓ 数据加载完成: {_candles.Count} 根K线");
        Console.WriteLine($"  数据范围: {_candles.First().DateTime:yyyy-MM-dd} 至 {_candles.Last().DateTime:yyyy-MM-dd}\n");

        // 定义参数搜索空间  
        var parameterSpace = new ParameterSpace
        {
            // Pin Bar形态参数 - 使用更宽松的条件
            MaxBodyPercentage = [25, 30, 35, 40, 45, 50],
            MinLongerWickPercentage = [40, 45, 50, 55, 60, 65, 70],
            MaxShorterWickPercentage = [10, 15, 20, 25, 30, 35, 40],
            
            // EMA相关参数
            NearEmaThreshold = [0.8m, 1.0m, 1.1m, 1.2m, 1.5m, 2.0m, 2.5m,3.0m],
            
            // 止损止盈参数
            StopLossAtrRatio = [1.0m, 1.5m, 2.0m],
            RiskRewardRatio = [1.5m, 2.0m, 2.5m],
            
            // 风控参数
            MaxLossPerTradePercent = [0.5m, 1.0m]
        };

        var totalCombinations = parameterSpace.GetTotalCombinations();
        Console.WriteLine($"总共需要测试 {totalCombinations} 种参数组合");
        Console.WriteLine($"预计耗时: {totalCombinations * 0.5 / 60.0:F1} 分钟 (假设每次0.5秒)\n");
        Console.WriteLine("按回车键开始优化...");
        Console.ReadLine();

        var startTime = DateTime.Now;
        var tested = 0;

        // 遍历所有参数组合
        await foreach (var parameters in parameterSpace.GetCombinations())
        {
            tested++;
            
            try
            {
                var result = RunBacktest(parameters);
                
                if (tested <= 5)
                {
                    Console.WriteLine($"  测试#{tested}: MaxBody={parameters.MaxBodyPercentage}%, MinWick={parameters.MinLongerWickPercentage}%");
                    Console.WriteLine($"    结果: 交易数={result.TotalTrades}, 收益率={result.TotalReturnRate:F2}%");
                }
                
                if (result != null)
                {
                    _results.Add(result);
                    
                    if (tested % 100 == 0)
                    {
                        Console.WriteLine($"  最新: 收益率={result.TotalReturnRate:F2}%, 胜率={result.WinRate:F2}%, 交易数={result.TotalTrades}");
                    }
                }
            }
            catch (DivideByZeroException)
            {
                // 某些参数组合可能不产生交易，跳过
                if (tested <= 5)
                {
                    Console.WriteLine($"  测试#{tested}: 除零异常 (可能没有产生交易)");
                }
            }
            catch (Exception ex)
            {
                if (tested <= 10 || tested % 100 == 0)
                {
                    Console.WriteLine($"  ✗ 测试#{tested}错误: {ex.Message}");
                    if (tested <= 3)
                    {
                        Console.WriteLine($"      Stack: {ex.StackTrace?.Split('\n').FirstOrDefault()}");
                    }
                }
            }
            
            if (tested % 100 == 0)
            {
                var elapsedSec = (DateTime.Now - startTime).TotalSeconds;
                var avgTime = elapsedSec / tested;
                var remaining = (totalCombinations - tested) * avgTime / 60;
                Console.WriteLine($"\n[进度: {tested}/{totalCombinations} ({tested * 100.0 / totalCombinations:F1}%)] 预计剩余: {remaining:F1}分钟");
                Console.WriteLine($"  已收集有效结果: {_results.Count}");
            }

            // 每500次保存一次中间结果
            if (tested % 500 == 0)
            {
                await SaveResults($"{ResultsDirectory}/checkpoint_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            }
        }

        var elapsed = DateTime.Now - startTime;
        Console.WriteLine($"\n\n优化完成! 耗时: {elapsed.TotalMinutes:F1} 分钟");
        Console.WriteLine($"成功测试: {_results.Count}/{totalCombinations}");

        // 保存最终结果
        await SaveResults($"{ResultsDirectory}/final_{DateTime.Now:yyyyMMdd_HHmmss}.json");

        // 显示最佳结果
        ShowTopResults(10);
    }

    private static OptimizationResult? RunBacktest(BacktestParameters parameters)
    {
        var config = new StrategyConfig
        {
            Symbol = "XAUUSD",
            CsvFilter = "",
            ContractSize = 100,
            
            // Pin Bar参数
            MaxBodyPercentage = parameters.MaxBodyPercentage,
            MinLongerWickPercentage = parameters.MinLongerWickPercentage,
            MaxShorterWickPercentage = parameters.MaxShorterWickPercentage,
            MinLowerWickAtrRatio = 0,
            Threshold = 0.8m,
            
            // EMA参数
            BaseEma = 200,
            EmaList = new List<int> { 20, 60, 80, 100, 200 },
            NearEmaThreshold = parameters.NearEmaThreshold,
            AtrPeriod = 14,
            
            // 止损止盈参数
            StopLossAtrRatio = parameters.StopLossAtrRatio,
            RiskRewardRatio = parameters.RiskRewardRatio,
            
            // 交易时间
            NoTradingHoursLimit = true,
            StartTradingHour = 5,
            EndTradingHour = 11,
            RequirePinBarDirectionMatch = false
        };

        var accountSettings = new AccountSettings
        {
            InitialCapital = 100000,
            Leverage = 30,
            MaxLossPerTradePercent = (double)parameters.MaxLossPerTradePercent,
            MaxDailyLossPercent = 3
        };

        // 创建策略并执行回测（使用预加载的数据）
        var strategy = new PinBarStrategy(config);
        var backtestEngine = new BacktestEngine(strategy);
        var result = backtestEngine.RunBacktest(_candles, config, accountSettings);

        return new OptimizationResult
        {
            Parameters = parameters,
            TotalTrades = result.OverallMetrics.TotalTrades,
            WinRate = result.OverallMetrics.WinRate,
            TotalReturnRate = result.OverallMetrics.TotalReturnRate,
            TotalProfit = result.OverallMetrics.TotalProfit,
            MaxDrawdown = result.OverallMetrics.MaxDrawdown,
            AvgWin = 0,
            AvgLoss = 0,
            SharpeRatio = 0
        };
    }

    private static async Task SaveResults(string filename)
    {
        var json = JsonSerializer.Serialize(_results, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await File.WriteAllTextAsync(filename, json);
        Console.WriteLine($"\n结果已保存到: {filename}");
    }

    private static void ShowTopResults(int topN)
    {
        Console.WriteLine($"\n=== Top {topN} 参数组合 (按收益率排序) ===\n");
        
        var topResults = _results
            .Where(r => r.TotalTrades >= 50) // 至少50笔交易
            .OrderByDescending(r => r.TotalReturnRate)
            .Take(topN);

        var rank = 1;
        foreach (var result in topResults)
        {
            Console.WriteLine($"#{rank++} 收益率: {result.TotalReturnRate:F2}%");
            Console.WriteLine($"   胜率: {result.WinRate:F2}%, 交易数: {result.TotalTrades}, 最大回撤: ${result.MaxDrawdown:F2}");
            Console.WriteLine($"   参数: MaxBody={result.Parameters.MaxBodyPercentage}%, MinWick={result.Parameters.MinLongerWickPercentage}%");
            Console.WriteLine($"         NearEma={result.Parameters.NearEmaThreshold}, SL={result.Parameters.StopLossAtrRatio}, RR={result.Parameters.RiskRewardRatio}");
            Console.WriteLine($"         MaxLoss={result.Parameters.MaxLossPerTradePercent}%\n");
        }
    }
}

public class ParameterSpace
{
    public int[] MaxBodyPercentage { get; set; } = Array.Empty<int>();
    public int[] MinLongerWickPercentage { get; set; } = Array.Empty<int>();
    public int[] MaxShorterWickPercentage { get; set; } = Array.Empty<int>();
    public decimal[] NearEmaThreshold { get; set; } = Array.Empty<decimal>();
    public decimal[] StopLossAtrRatio { get; set; } = Array.Empty<decimal>();
    public decimal[] RiskRewardRatio { get; set; } = Array.Empty<decimal>();
    public decimal[] MaxLossPerTradePercent { get; set; } = Array.Empty<decimal>();

    public int GetTotalCombinations()
    {
        return MaxBodyPercentage.Length
            * MinLongerWickPercentage.Length
            * MaxShorterWickPercentage.Length
            * NearEmaThreshold.Length
            * StopLossAtrRatio.Length
            * RiskRewardRatio.Length
            * MaxLossPerTradePercent.Length;
    }

    public async IAsyncEnumerable<BacktestParameters> GetCombinations()
    {
        foreach (var maxBody in MaxBodyPercentage)
        foreach (var minWick in MinLongerWickPercentage)
        foreach (var maxWick in MaxShorterWickPercentage)
        foreach (var nearEma in NearEmaThreshold)
        foreach (var stopLoss in StopLossAtrRatio)
        foreach (var riskReward in RiskRewardRatio)
        foreach (var maxLoss in MaxLossPerTradePercent)
        {
            yield return new BacktestParameters
            {
                MaxBodyPercentage = maxBody,
                MinLongerWickPercentage = minWick,
                MaxShorterWickPercentage = maxWick,
                NearEmaThreshold = nearEma,
                StopLossAtrRatio = stopLoss,
                RiskRewardRatio = riskReward,
                MaxLossPerTradePercent = maxLoss
            };
            await Task.Yield();
        }
    }
}

public class BacktestParameters
{
    public int MaxBodyPercentage { get; set; }
    public int MinLongerWickPercentage { get; set; }
    public int MaxShorterWickPercentage { get; set; }
    public decimal NearEmaThreshold { get; set; }
    public decimal StopLossAtrRatio { get; set; }
    public decimal RiskRewardRatio { get; set; }
    public decimal MaxLossPerTradePercent { get; set; }
}

public class OptimizationResult
{
    public BacktestParameters Parameters { get; set; } = null!;
    public int TotalTrades { get; set; }
    public decimal WinRate { get; set; }
    public decimal TotalReturnRate { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal AvgWin { get; set; }
    public decimal AvgLoss { get; set; }
    public decimal SharpeRatio { get; set; }
}
