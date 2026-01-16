namespace Trading.Backtest.ParameterOptimizer.Models;

public class BacktestParameters
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public int MaxBodyPercentage { get; set; }
    public int MinLongerWickPercentage { get; set; }
    public int MaxShorterWickPercentage { get; set; }
    public decimal NearEmaThreshold { get; set; }
    public decimal StopLossAtrRatio { get; set; }
    public decimal RiskRewardRatio { get; set; }
    public decimal MaxLossPerTradePercent { get; set; }
}
