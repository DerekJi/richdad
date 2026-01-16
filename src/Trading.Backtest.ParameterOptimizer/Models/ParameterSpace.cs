namespace Trading.Backtest.ParameterOptimizer.Models;

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
