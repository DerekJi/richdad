namespace Trading.Core.RiskManagement.Models;

/// <summary>
/// Detailed breakdown of position size calculation
/// </summary>
public class CalculationDetail
{
    /// <summary>
    /// Entry price for the trade
    /// </summary>
    public decimal EntryPrice { get; set; }

    /// <summary>
    /// Stop loss price
    /// </summary>
    public decimal StopLoss { get; set; }

    /// <summary>
    /// Risk in pips/points
    /// </summary>
    public decimal PipsRisk { get; set; }

    /// <summary>
    /// Risk amount per lot in account currency
    /// </summary>
    public decimal PerLotRisk { get; set; }

    /// <summary>
    /// Maximum daily loss limit
    /// </summary>
    public decimal DailyLossLimit { get; set; }

    /// <summary>
    /// Current daily loss
    /// </summary>
    public decimal DailyLoss { get; set; }

    /// <summary>
    /// Remaining daily risk allowance
    /// </summary>
    public decimal DailyRemainingRisk { get; set; }

    /// <summary>
    /// Maximum total loss limit
    /// </summary>
    public decimal TotalLossLimit { get; set; }

    /// <summary>
    /// Current total loss
    /// </summary>
    public decimal TotalLoss { get; set; }

    /// <summary>
    /// Remaining total risk allowance
    /// </summary>
    public decimal TotalRemainingRisk { get; set; }

    /// <summary>
    /// Requested risk per trade
    /// </summary>
    public decimal RequestedRiskPerTrade { get; set; }

    /// <summary>
    /// Actual allowed risk for this trade (may be lower than requested)
    /// </summary>
    public decimal AllowedRiskForTrade { get; set; }

    /// <summary>
    /// Trading day for this calculation
    /// </summary>
    public DateTime TradingDay { get; set; }

    /// <summary>
    /// Server time used for trading day calculation
    /// </summary>
    public DateTime ServerTime { get; set; }

    /// <summary>
    /// Contract size of the instrument
    /// </summary>
    public decimal ContractSize { get; set; }

    /// <summary>
    /// Tick size of the instrument
    /// </summary>
    public decimal TickSize { get; set; }
}
