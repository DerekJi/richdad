namespace Trading.Data.Configuration;

public class StrategySettings
{
    public string Symbol { get; set; } = string.Empty;
    public double AtrMultiplierStopLoss { get; set; }
    public double AtrMultiplierTakeProfit { get; set; }
    public double MinLowerWickAtrRatio { get; set; }
    public double Threshold { get; set; }
    public double NearEmaThreshold { get; set; }
    public double RiskRewardRatio { get; set; }
    public double StopLossAtrRatio { get; set; }
    public int StartTradingHour { get; set; }
    public int EndTradingHour { get; set; }
    public bool NoTradingHoursLimit { get; set; } = false;
    public bool RequirePinBarDirectionMatch { get; set; }
    public double MaxBodyPercentage { get; set; }
    public double MinLongerWickPercentage { get; set; }
    public double MaxShorterWickPercentage { get; set; }
    public List<int> EmaList { get; set; } = new();
}
