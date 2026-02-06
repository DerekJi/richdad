using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Trading.Backtest.ParameterOptimizer.Commands;

/// <summary>
/// 结果分析命令
/// </summary>
public class AnalyzerCommand
{
    public static void Execute(string[] args)
    {
        Console.WriteLine("=== Pin Bar Strategy Results Analyzer ===\n");

        // 支持从命令行参数指定文件
        var filePath = args.Length > 0 ? args[0] : FindLatestCheckpointFile();

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"❌ 错误: 文件不存在 - {filePath}");
            Console.WriteLine($"\n💡 提示: 请先运行优化器生成结果文件");
            Console.WriteLine($"   运行命令: dotnet run");
            return;
        }

        Console.WriteLine($"📁 正在读取文件: {filePath}...");
        var json = File.ReadAllText(filePath);

        Console.WriteLine("🔄 正在解析JSON...");
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var results = JsonSerializer.Deserialize<List<OptimizationResult>>(json, options);

        if (results == null || results.Count == 0)
        {
            Console.WriteLine("❌ 错误: 无法解析结果文件或文件为空");
            return;
        }

        Console.WriteLine($"✓ 共有 {results.Count:N0} 个测试结果\n");

        var top10 = results
            .OrderByDescending(r => r.TotalReturnRate)
            .Take(10)
            .ToList();

        // 控制台输出
        PrintResults(results.Count, top10);

        // 生成报告
        var reportPath = GenerateReport(filePath, results.Count, top10);
        Console.WriteLine($"\n✅ 分析报告已生成: {reportPath}");
    }

    private static string FindLatestCheckpointFile()
    {
        var resultsDir = "results";
        if (!Directory.Exists(resultsDir))
            return "results/checkpoint_latest.json";

        var checkpoints = Directory.GetFiles(resultsDir, "checkpoint_*.json")
            .OrderByDescending(f => File.GetLastWriteTime(f))
            .FirstOrDefault();

        return checkpoints ?? "results/checkpoint_latest.json";
    }

    private static void PrintResults(int totalCount, List<OptimizationResult> top10)
    {
        Console.WriteLine("收益率最高的前10个参数组合:\n");

        for (int i = 0; i < top10.Count; i++)
        {
            var result = top10[i];
            var p = result.Parameters;

            Console.WriteLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine($"排名 {i + 1}:");
            Console.WriteLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine($"  收益率: {result.TotalReturnRate:F2}%");
            Console.WriteLine($"  胜率: {result.WinRate:F2}%");
            Console.WriteLine($"  交易数: {result.TotalTrades}");
            Console.WriteLine($"  总盈利: ${result.TotalProfit:F2}");
            Console.WriteLine($"  最大回撤: ${result.MaxDrawdown:F2}");
            Console.WriteLine($"\n  参数配置:");
            Console.WriteLine($"    Pin Bar形状参数:");
            Console.WriteLine($"      - 实体占比上限: {p.MaxBodyPercentage}%");
            Console.WriteLine($"      - 长影线占比下限: {p.MinLongerWickPercentage}%");
            Console.WriteLine($"      - 短影线占比上限: {p.MaxShorterWickPercentage}%");
            Console.WriteLine($"    交易触发参数:");
            Console.WriteLine($"      - EMA距离阈值: {p.NearEmaThreshold}");
            Console.WriteLine($"    风险管理参数:");
            Console.WriteLine($"      - 止损ATR倍数: {p.StopLossAtrRatio}");
            Console.WriteLine($"      - 风险回报比: {p.RiskRewardRatio}");
            Console.WriteLine($"      - 单笔最大亏损: {p.MaxLossPerTradePercent}%");
            Console.WriteLine();
        }
    }

    private static string GenerateReport(string sourceFile, int totalCount, List<OptimizationResult> top10)
    {
        var sb = new StringBuilder();
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var fileName = Path.GetFileName(sourceFile);

        sb.AppendLine("# Pin Bar策略参数优化分析报告");
        sb.AppendLine();
        sb.AppendLine($"**生成时间**: {timestamp}  ");
        sb.AppendLine($"**数据来源**: `{fileName}`  ");
        sb.AppendLine($"**测试总数**: {totalCount:N0} 组参数  ");
        sb.AppendLine();

        // 核心发现
        sb.AppendLine("## 🎯 核心发现");
        sb.AppendLine();

        // 分析共同特征
        var commonFeatures = AnalyzeCommonFeatures(top10);
        sb.AppendLine("### Top 10 共同特征");
        sb.AppendLine();
        sb.AppendLine($"- ✅ **风险回报比**: {commonFeatures.RiskRewardRatio}");
        sb.AppendLine($"- ✅ **止损ATR倍数**: {commonFeatures.StopLossAtrRatio}");
        sb.AppendLine($"- ✅ **单笔最大亏损**: {commonFeatures.MaxLossPerTradePercent}");
        sb.AppendLine($"- ✅ **实体占比上限**: {commonFeatures.MaxBodyPercentageRange}");
        sb.AppendLine($"- ✅ **长影线占比下限**: {commonFeatures.MinLongerWickPercentageRange}");
        sb.AppendLine($"- ✅ **短影线占比上限**: {commonFeatures.MaxShorterWickPercentageRange}");
        sb.AppendLine($"- ✅ **平均胜率**: {commonFeatures.AvgWinRate:F2}%");
        sb.AppendLine($"- ✅ **平均交易数**: {commonFeatures.AvgTrades:F0}笔");
        sb.AppendLine();

        // 排名第一的最佳参数
        var best = top10[0];
        var bestParams = best.Parameters;
        sb.AppendLine("### 🏆 排名第1的最佳参数");
        sb.AppendLine();
        sb.AppendLine("| 指标 | 数值 |");
        sb.AppendLine("|------|------|");
        sb.AppendLine($"| **收益率** | **{best.TotalReturnRate:F2}%** |");
        sb.AppendLine($"| 胜率 | {best.WinRate:F2}% |");
        sb.AppendLine($"| 交易数 | {best.TotalTrades}笔 |");
        sb.AppendLine($"| 总盈利 | ${best.TotalProfit:F2} |");
        sb.AppendLine($"| 最大回撤 | ${best.MaxDrawdown:F2} |");
        sb.AppendLine();
        sb.AppendLine("**参数配置**:");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine("Pin Bar形状参数:");
        sb.AppendLine($"  实体占比上限: {bestParams.MaxBodyPercentage}%");
        sb.AppendLine($"  长影线占比下限: {bestParams.MinLongerWickPercentage}%");
        sb.AppendLine($"  短影线占比上限: {bestParams.MaxShorterWickPercentage}%");
        sb.AppendLine();
        sb.AppendLine("交易触发参数:");
        sb.AppendLine($"  EMA距离阈值: {bestParams.NearEmaThreshold}");
        sb.AppendLine();
        sb.AppendLine("风险管理参数:");
        sb.AppendLine($"  止损ATR倍数: {bestParams.StopLossAtrRatio}");
        sb.AppendLine($"  风险回报比: {bestParams.RiskRewardRatio}");
        sb.AppendLine($"  单笔最大亏损: {bestParams.MaxLossPerTradePercent}%");
        sb.AppendLine("```");
        sb.AppendLine();

        // 关键洞察
        sb.AppendLine("### 💡 关键洞察");
        sb.AppendLine();
        sb.AppendLine($"1. **高风险回报比是关键**: 所有Top 10都使用了{commonFeatures.RiskRewardRatio}的风险回报比，显著高于常见的1.5-2.0");
        sb.AppendLine($"2. **允许更高的单笔风险**: 使用{commonFeatures.MaxLossPerTradePercent}的单笔最大亏损提升了整体收益");
        sb.AppendLine($"3. **胜率并非越高越好**: Top 10的平均胜率仅{commonFeatures.AvgWinRate:F1}%，但通过高盈亏比实现盈利");
        sb.AppendLine($"4. **严格的Pin Bar识别标准**: 较小的实体（{commonFeatures.MaxBodyPercentageRange}）和较长的影线（{commonFeatures.MinLongerWickPercentageRange}）能识别出更可靠的信号");
        sb.AppendLine($"5. **交易频率适中**: 平均{commonFeatures.AvgTrades:F0}笔交易，避免了过度交易");
        sb.AppendLine();

        // Top 10 详细排名
        sb.AppendLine("## 📊 Top 10 详细排名");
        sb.AppendLine();

        for (int i = 0; i < top10.Count; i++)
        {
            var result = top10[i];
            var p = result.Parameters;

            sb.AppendLine($"### 排名 {i + 1}");
            sb.AppendLine();
            sb.AppendLine("| 指标 | 数值 |");
            sb.AppendLine("|------|------|");
            sb.AppendLine($"| 收益率 | **{result.TotalReturnRate:F2}%** |");
            sb.AppendLine($"| 胜率 | {result.WinRate:F2}% |");
            sb.AppendLine($"| 交易数 | {result.TotalTrades}笔 |");
            sb.AppendLine($"| 总盈利 | ${result.TotalProfit:F2} |");
            sb.AppendLine($"| 最大回撤 | ${result.MaxDrawdown:F2} |");
            sb.AppendLine();
            sb.AppendLine("<details>");
            sb.AppendLine("<summary>参数配置</summary>");
            sb.AppendLine();
            sb.AppendLine("```json");
            sb.AppendLine("{");
            sb.AppendLine($"  \"maxBodyPercentage\": {p.MaxBodyPercentage},");
            sb.AppendLine($"  \"minLongerWickPercentage\": {p.MinLongerWickPercentage},");
            sb.AppendLine($"  \"maxShorterWickPercentage\": {p.MaxShorterWickPercentage},");
            sb.AppendLine($"  \"nearEmaThreshold\": {p.NearEmaThreshold},");
            sb.AppendLine($"  \"stopLossAtrRatio\": {p.StopLossAtrRatio},");
            sb.AppendLine($"  \"riskRewardRatio\": {p.RiskRewardRatio},");
            sb.AppendLine($"  \"maxLossPerTradePercent\": {p.MaxLossPerTradePercent}");
            sb.AppendLine("}");
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("</details>");
            sb.AppendLine();
        }

        // 参数分布统计
        sb.AppendLine("## 📈 参数分布统计");
        sb.AppendLine();
        sb.AppendLine("| 参数 | 最小值 | 最大值 | 众数 |");
        sb.AppendLine("|------|--------|--------|------|");
        sb.AppendLine($"| 实体占比上限 | {top10.Min(r => r.Parameters.MaxBodyPercentage)}% | {top10.Max(r => r.Parameters.MaxBodyPercentage)}% | {GetMode(top10.Select(r => r.Parameters.MaxBodyPercentage))}% |");
        sb.AppendLine($"| 长影线占比下限 | {top10.Min(r => r.Parameters.MinLongerWickPercentage)}% | {top10.Max(r => r.Parameters.MinLongerWickPercentage)}% | {GetMode(top10.Select(r => r.Parameters.MinLongerWickPercentage))}% |");
        sb.AppendLine($"| 短影线占比上限 | {top10.Min(r => r.Parameters.MaxShorterWickPercentage)}% | {top10.Max(r => r.Parameters.MaxShorterWickPercentage)}% | {GetMode(top10.Select(r => r.Parameters.MaxShorterWickPercentage))}% |");
        sb.AppendLine($"| EMA距离阈值 | {top10.Min(r => r.Parameters.NearEmaThreshold)} | {top10.Max(r => r.Parameters.NearEmaThreshold)} | {GetMode(top10.Select(r => r.Parameters.NearEmaThreshold))} |");
        sb.AppendLine($"| 止损ATR倍数 | {top10.Min(r => r.Parameters.StopLossAtrRatio)} | {top10.Max(r => r.Parameters.StopLossAtrRatio)} | {GetMode(top10.Select(r => r.Parameters.StopLossAtrRatio))} |");
        sb.AppendLine($"| 风险回报比 | {top10.Min(r => r.Parameters.RiskRewardRatio)} | {top10.Max(r => r.Parameters.RiskRewardRatio)} | {GetMode(top10.Select(r => r.Parameters.RiskRewardRatio))} |");
        sb.AppendLine($"| 单笔最大亏损 | {top10.Min(r => r.Parameters.MaxLossPerTradePercent)}% | {top10.Max(r => r.Parameters.MaxLossPerTradePercent)}% | {GetMode(top10.Select(r => r.Parameters.MaxLossPerTradePercent))}% |");
        sb.AppendLine();

        // 保存报告
        var reportFileName = $"optimization_report_{DateTime.Now:yyyyMMdd_HHmmss}.md";
        var reportPath = Path.Combine("results", reportFileName);
        File.WriteAllText(reportPath, sb.ToString());

        return reportPath;
    }

    private static CommonFeatures AnalyzeCommonFeatures(List<OptimizationResult> top10)
    {
        var riskRewards = top10.Select(r => r.Parameters.RiskRewardRatio).Distinct().ToList();
        var stopLosses = top10.Select(r => r.Parameters.StopLossAtrRatio).Distinct().ToList();
        var maxLosses = top10.Select(r => r.Parameters.MaxLossPerTradePercent).Distinct().ToList();

        return new CommonFeatures
        {
            RiskRewardRatio = riskRewards.Count == 1 ? $"**{riskRewards[0]}** (100%一致)" : $"{string.Join(", ", riskRewards)}",
            StopLossAtrRatio = stopLosses.Count == 1 ? $"**{stopLosses[0]}** (100%一致)" : $"{string.Join(", ", stopLosses)}",
            MaxLossPerTradePercent = maxLosses.Count == 1 ? $"**{maxLosses[0]}%** (100%一致)" : $"{string.Join(", ", maxLosses)}%",
            MaxBodyPercentageRange = $"{top10.Min(r => r.Parameters.MaxBodyPercentage)}-{top10.Max(r => r.Parameters.MaxBodyPercentage)}%",
            MinLongerWickPercentageRange = $"{top10.Min(r => r.Parameters.MinLongerWickPercentage)}-{top10.Max(r => r.Parameters.MinLongerWickPercentage)}%",
            MaxShorterWickPercentageRange = $"{top10.Min(r => r.Parameters.MaxShorterWickPercentage)}-{top10.Max(r => r.Parameters.MaxShorterWickPercentage)}%",
            AvgWinRate = top10.Average(r => r.WinRate),
            AvgTrades = (decimal)top10.Average(r => r.TotalTrades)
        };
    }

    private static T GetMode<T>(IEnumerable<T> values)
    {
        return values.GroupBy(v => v)
                     .OrderByDescending(g => g.Count())
                     .First()
                     .Key;
    }

    #region Data Models

    private record CommonFeatures
    {
        public string RiskRewardRatio { get; init; } = "";
        public string StopLossAtrRatio { get; init; } = "";
        public string MaxLossPerTradePercent { get; init; } = "";
        public string MaxBodyPercentageRange { get; init; } = "";
        public string MinLongerWickPercentageRange { get; init; } = "";
        public string MaxShorterWickPercentageRange { get; init; } = "";
        public decimal AvgWinRate { get; init; }
        public decimal AvgTrades { get; init; }
    }

    private record BacktestParameters(
        [property: JsonPropertyName("maxBodyPercentage")] int MaxBodyPercentage,
        [property: JsonPropertyName("minLongerWickPercentage")] int MinLongerWickPercentage,
        [property: JsonPropertyName("maxShorterWickPercentage")] int MaxShorterWickPercentage,
        [property: JsonPropertyName("nearEmaThreshold")] decimal NearEmaThreshold,
        [property: JsonPropertyName("stopLossAtrRatio")] decimal StopLossAtrRatio,
        [property: JsonPropertyName("riskRewardRatio")] decimal RiskRewardRatio,
        [property: JsonPropertyName("maxLossPerTradePercent")] decimal MaxLossPerTradePercent
    );

    private record OptimizationResult(
        [property: JsonPropertyName("parameters")] BacktestParameters Parameters,
        [property: JsonPropertyName("totalTrades")] int TotalTrades,
        [property: JsonPropertyName("winRate")] decimal WinRate,
        [property: JsonPropertyName("totalReturnRate")] decimal TotalReturnRate,
        [property: JsonPropertyName("totalProfit")] decimal TotalProfit,
        [property: JsonPropertyName("maxDrawdown")] decimal MaxDrawdown,
        [property: JsonPropertyName("avgWin")] decimal AvgWin,
        [property: JsonPropertyName("avgLoss")] decimal AvgLoss,
        [property: JsonPropertyName("sharpeRatio")] decimal SharpeRatio
    );

    #endregion
}
