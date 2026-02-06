using System.Text.Json;
using Trading.Backtest.ParameterOptimizer.Models;

namespace Trading.Backtest.ParameterOptimizer.Services;

public class ResultsManager
{
    private readonly string _resultsDirectory;
    private readonly List<OptimizationResult> _results = new();

    public ResultsManager(string resultsDirectory = "results")
    {
        _resultsDirectory = resultsDirectory;
        Directory.CreateDirectory(_resultsDirectory);
    }

    public IReadOnlyList<OptimizationResult> Results => _results.AsReadOnly();

    public void AddResult(OptimizationResult result)
    {
        _results.Add(result);
    }

    public async Task SaveResults(string filename)
    {
        var filePath = Path.Combine(_resultsDirectory, filename);
        var json = JsonSerializer.Serialize(_results, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await File.WriteAllTextAsync(filePath, json);
        Console.WriteLine($"\n结果已保存到: {filePath}");
    }

    public void ShowTopResults(int topN)
    {
        Console.WriteLine($"\n=== Top {topN} 参数组合 (按收益率排序) ===\n");

        var topResults = _results
            .Where(r => r.TotalTrades >= 50) // 至少50笔交易
            .OrderByDescending(r => r.TotalReturnRate)
            .Take(topN);

        var rank = 1;
        foreach (var result in topResults)
        {
            Console.WriteLine($"#{rank++} {result.Parameters.Id} 收益率: {result.TotalReturnRate:F2}%");
            Console.WriteLine($"  收益率: {result.TotalReturnRate:F2}%");
            Console.WriteLine($"   胜率: {result.WinRate:F2}%, 交易数: {result.TotalTrades}, 最大回撤: ${result.MaxDrawdown:F2}");
            Console.WriteLine($"   参数: MaxBody={result.Parameters.MaxBodyPercentage}%, MinWick={result.Parameters.MinLongerWickPercentage}%");
            Console.WriteLine($"         NearEma={result.Parameters.NearEmaThreshold}, SL={result.Parameters.StopLossAtrRatio}, RR={result.Parameters.RiskRewardRatio}");
            Console.WriteLine($"         MaxLoss={result.Parameters.MaxLossPerTradePercent}%\n");
        }
    }
}
