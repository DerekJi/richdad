namespace Trading.Core.RiskManagement.Models;

/// <summary>
/// Contains all risk-related parameters for position sizing calculation
/// </summary>
public class RiskParameters
{
    /// <summary>
    /// Current account balance
    /// </summary>
    public decimal AccountBalance { get; set; }

    /// <summary>
    /// Initial account balance (for prop firm rule calculations)
    /// </summary>
    public decimal InitialBalance { get; set; }

    /// <summary>
    /// Prop firm rule name to use (e.g., "BlueGuardian", "FTMO")
    /// If null, uses custom percentages
    /// </summary>
    public string? PropFirmRule { get; set; }

    /// <summary>
    /// Custom maximum daily loss percentage (used if PropFirmRule is null)
    /// </summary>
    public decimal? CustomDailyLossPercent { get; set; }

    /// <summary>
    /// Custom maximum total loss percentage (used if PropFirmRule is null)
    /// </summary>
    public decimal? CustomTotalLossPercent { get; set; }

    /// <summary>
    /// Custom server timezone offset (used if PropFirmRule is null)
    /// </summary>
    public int? CustomServerTimeZoneOffset { get; set; }

    /// <summary>
    /// Maximum risk per trade as percentage (e.g., 1.0 for 1%)
    /// </summary>
    public decimal? RiskPercentPerTrade { get; set; }

    /// <summary>
    /// Maximum risk per trade as absolute amount
    /// </summary>
    public decimal? RiskAmountPerTrade { get; set; }

    /// <summary>
    /// Total loss today in account currency
    /// </summary>
    public decimal TodayLoss { get; set; }

    /// <summary>
    /// Total loss since account start
    /// </summary>
    public decimal TotalLoss { get; set; }

    /// <summary>
    /// Last date when daily loss was reset
    /// </summary>
    public DateTime? LastResetDate { get; set; }

    /// <summary>
    /// Gets the effective risk amount per trade
    /// </summary>
    public decimal GetEffectiveRiskPerTrade()
    {
        if (RiskAmountPerTrade.HasValue && RiskAmountPerTrade.Value > 0)
        {
            return RiskAmountPerTrade.Value;
        }

        if (RiskPercentPerTrade.HasValue && RiskPercentPerTrade.Value > 0)
        {
            return AccountBalance * (RiskPercentPerTrade.Value / 100m);
        }

        throw new InvalidOperationException("Either RiskAmountPerTrade or RiskPercentPerTrade must be specified");
    }

    /// <summary>
    /// Validates that required parameters are set
    /// </summary>
    public void Validate()
    {
        if (AccountBalance <= 0)
            throw new ArgumentException("Account balance must be greater than 0", nameof(AccountBalance));

        if (InitialBalance <= 0)
            throw new ArgumentException("Initial balance must be greater than 0", nameof(InitialBalance));

        if (PropFirmRule == null)
        {
            if (!CustomDailyLossPercent.HasValue || CustomDailyLossPercent.Value <= 0)
                throw new ArgumentException("CustomDailyLossPercent must be set when PropFirmRule is null");

            if (!CustomTotalLossPercent.HasValue || CustomTotalLossPercent.Value <= 0)
                throw new ArgumentException("CustomTotalLossPercent must be set when PropFirmRule is null");
        }

        if (!RiskAmountPerTrade.HasValue && !RiskPercentPerTrade.HasValue)
            throw new ArgumentException("Either RiskAmountPerTrade or RiskPercentPerTrade must be specified");

        if (TodayLoss < 0)
            throw new ArgumentException("TodayLoss cannot be negative", nameof(TodayLoss));

        if (TotalLoss < 0)
            throw new ArgumentException("TotalLoss cannot be negative", nameof(TotalLoss));
    }
}
