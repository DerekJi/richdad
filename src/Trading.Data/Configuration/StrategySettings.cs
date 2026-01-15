namespace Trading.Data.Configuration;

using Trading.Data.Models;

public class StrategySettings
{
    public string Symbol { get; set; } = string.Empty;
    public string CsvFilter { get; set; } = string.Empty;
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

    /// <summary>
    /// 转换为 StrategyConfig（业务模型）
    /// </summary>
    public StrategyConfig ToStrategyConfig(string strategyName, int baseEma, int atrPeriod)
    {
        var contractSize = Symbol switch
        {
            "XAUUSD" => 100,
            "XAGUSD" => 1000,
            _ => 100
        };

        // 确保 EmaList 包含 BaseEma
        var emaList = new List<int>(EmaList ?? new List<int>());
        if (baseEma > 0 && !emaList.Contains(baseEma))
        {
            emaList.Add(baseEma);
        }

        return new StrategyConfig
        {
            StrategyName = strategyName,
            Symbol = Symbol,
            CsvFilter = CsvFilter,
            ContractSize = contractSize,
            BaseEma = baseEma,
            AtrPeriod = atrPeriod,
            RiskRewardRatio = (decimal)RiskRewardRatio,
            StopLossAtrRatio = (decimal)StopLossAtrRatio,
            MinLowerWickAtrRatio = (decimal)MinLowerWickAtrRatio,
            Threshold = (decimal)Threshold,
            NearEmaThreshold = (decimal)NearEmaThreshold,
            StartTradingHour = StartTradingHour,
            EndTradingHour = EndTradingHour,
            NoTradingHoursLimit = NoTradingHoursLimit,
            RequirePinBarDirectionMatch = RequirePinBarDirectionMatch,
            MaxBodyPercentage = (decimal)MaxBodyPercentage,
            MinLongerWickPercentage = (decimal)MinLongerWickPercentage,
            MaxShorterWickPercentage = (decimal)MaxShorterWickPercentage,
            EmaList = emaList
        };
    }
}
