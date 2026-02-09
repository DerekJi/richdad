using Trading.Backtest.Data.Models;
namespace Trading.Strategy.Analyzer.Analyzers;

/// <summary>
/// 分析器接口 - 所有分析器的基础接口
/// </summary>
public interface IAnalyzer
{
    /// <summary>
    /// 运行分析
    /// </summary>
    Task RunAsync();
}
