using Trading.Backtest.Data.Models;
using Trading.Backtest.ParameterOptimizer.Models;

namespace Trading.Backtest.ParameterOptimizer.Services;

public class ParameterOptimizer
{
    private readonly BacktestExecutor _executor;
    private readonly ResultsManager _resultsManager;

    public ParameterOptimizer(BacktestExecutor executor, ResultsManager resultsManager)
    {
        _executor = executor;
        _resultsManager = resultsManager;
    }

    public async Task OptimizeAsync(ParameterSpace parameterSpace)
    {
        var totalCombinations = parameterSpace.GetTotalCombinations();
        Console.WriteLine($"总共需要测试 {totalCombinations} 种参数组合");
        Console.WriteLine($"预计耗时: {totalCombinations * 0.5 / 60.0:F1} 分钟 (假设每次0.5秒)\n");
        Console.WriteLine("按回车键开始优化...");
        Console.ReadLine();

        var startTime = DateTime.Now;
        var tested = 0;

        await foreach (var parameters in parameterSpace.GetCombinations())
        {
            tested++;

            try
            {
                var result = _executor.RunBacktest(parameters);

                if (tested <= 5)
                {
                    Console.WriteLine($"  测试#{tested}: MaxBody={parameters.MaxBodyPercentage}%, MinWick={parameters.MinLongerWickPercentage}%");
                    Console.WriteLine($"    结果: 交易数={result.TotalTrades}, 收益率={result.TotalReturnRate:F2}%");
                }

                if (result != null)
                {
                    _resultsManager.AddResult(result);

                    if (tested % 100 == 0)
                    {
                        Console.WriteLine($"  最新: 收益率={result.TotalReturnRate:F2}%, 胜率={result.WinRate:F2}%, 交易数={result.TotalTrades}");
                    }
                }
            }
            catch (DivideByZeroException)
            {
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
                Console.WriteLine($"  已收集有效结果: {_resultsManager.Results.Count}");
            }

            if (tested % 500 == 0)
            {
                await _resultsManager.SaveResults($"checkpoint_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            }
        }

        var elapsed = DateTime.Now - startTime;
        Console.WriteLine($"\n\n优化完成! 耗时: {elapsed.TotalMinutes:F1} 分钟");
        Console.WriteLine($"成功测试: {_resultsManager.Results.Count}/{totalCombinations}");

        await _resultsManager.SaveResults($"final_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        _resultsManager.ShowTopResults(10);
    }
}
