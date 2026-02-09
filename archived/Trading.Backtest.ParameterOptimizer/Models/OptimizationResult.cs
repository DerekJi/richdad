using Trading.Backtest.Data.Models;
namespace Trading.Backtest.ParameterOptimizer.Models;

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
