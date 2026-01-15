namespace TradingBacktest.Data.Configuration;

public class StrategySettings
{
    public string Symbol { get; set; } = string.Empty;
    public double RiskPercentage { get; set; }
    public int EmaFastPeriod { get; set; }
    public int EmaSlowPeriod { get; set; }
    public int AtrPeriod { get; set; }
    public double AtrMultiplierStopLoss { get; set; }
    public double AtrMultiplierTakeProfit { get; set; }
}
