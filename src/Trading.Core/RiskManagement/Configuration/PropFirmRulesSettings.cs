using Trading.Core.RiskManagement.Models;

namespace Trading.Core.RiskManagement.Configuration;

/// <summary>
/// Configuration settings for prop firm rules
/// </summary>
public class PropFirmRulesSettings
{
    public Dictionary<string, PropFirmRuleConfig> Rules { get; set; } = new();
}

/// <summary>
/// Individual prop firm rule configuration
/// </summary>
public class PropFirmRuleConfig
{
    public string Name { get; set; } = string.Empty;
    public decimal MaxDailyLossPercent { get; set; }
    public decimal MaxTotalLossPercent { get; set; }
    public int ServerTimeZoneOffset { get; set; }
    public string CalculationBase { get; set; } = "InitialBalance";
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Converts configuration to PropFirmRules model
    /// </summary>
    public PropFirmRules ToModel()
    {
        return new PropFirmRules
        {
            Name = Name,
            MaxDailyLossPercent = MaxDailyLossPercent,
            MaxTotalLossPercent = MaxTotalLossPercent,
            ServerTimeZoneOffset = ServerTimeZoneOffset,
            CalculationBase = Enum.Parse<Models.CalculationBase>(CalculationBase, true),
            IsActive = IsActive
        };
    }
}
