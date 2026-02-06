namespace Trading.Core.RiskManagement.Models;

/// <summary>
/// Represents the risk management rules for a prop trading firm
/// </summary>
public class PropFirmRules
{
    /// <summary>
    /// Name of the prop firm (e.g., "Blue Guardian", "FTMO")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Maximum daily loss percentage (e.g., 3.0 for 3%, 5.0 for 5%)
    /// </summary>
    public decimal MaxDailyLossPercent { get; set; }

    /// <summary>
    /// Maximum total loss percentage (e.g., 6.0 for 6%, 10.0 for 10%)
    /// </summary>
    public decimal MaxTotalLossPercent { get; set; }

    /// <summary>
    /// Server timezone offset in hours (e.g., 2 for UTC+2, -5 for UTC-5)
    /// </summary>
    public int ServerTimeZoneOffset { get; set; }

    /// <summary>
    /// Whether to calculate loss limits based on initial balance or current balance
    /// </summary>
    public CalculationBase CalculationBase { get; set; } = CalculationBase.InitialBalance;

    /// <summary>
    /// Whether this rule set is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets the current trading day based on server timezone
    /// </summary>
    public DateTime GetCurrentTradingDay()
    {
        var serverTime = DateTime.UtcNow.AddHours(ServerTimeZoneOffset);
        return serverTime.Date;
    }

    /// <summary>
    /// Calculates the maximum daily loss amount based on account balance
    /// </summary>
    public decimal GetMaxDailyLossAmount(decimal baseBalance)
    {
        return baseBalance * (MaxDailyLossPercent / 100m);
    }

    /// <summary>
    /// Calculates the maximum total loss amount based on account balance
    /// </summary>
    public decimal GetMaxTotalLossAmount(decimal baseBalance)
    {
        return baseBalance * (MaxTotalLossPercent / 100m);
    }
}

/// <summary>
/// Defines how loss limits are calculated
/// </summary>
public enum CalculationBase
{
    /// <summary>
    /// Calculate based on the initial account balance (common for prop firms)
    /// </summary>
    InitialBalance,

    /// <summary>
    /// Calculate based on the current account balance
    /// </summary>
    CurrentBalance
}
