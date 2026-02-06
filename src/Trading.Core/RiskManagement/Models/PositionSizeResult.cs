namespace Trading.Core.RiskManagement.Models;

/// <summary>
/// Result of position size calculation
/// </summary>
public class PositionSizeResult
{
    /// <summary>
    /// Whether trading is allowed based on risk rules
    /// </summary>
    public bool CanTrade { get; set; }

    /// <summary>
    /// Calculated position size in lots
    /// </summary>
    public decimal PositionSize { get; set; }

    /// <summary>
    /// Actual risk amount for this trade
    /// </summary>
    public decimal RiskAmount { get; set; }

    /// <summary>
    /// Reason for the decision (e.g., why trading is not allowed)
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Detailed calculation breakdown
    /// </summary>
    public CalculationDetail? Details { get; set; }

    /// <summary>
    /// Creates a result indicating trading is not allowed
    /// </summary>
    public static PositionSizeResult NotAllowed(string reason)
    {
        return new PositionSizeResult
        {
            CanTrade = false,
            PositionSize = 0,
            RiskAmount = 0,
            Reason = reason
        };
    }

    /// <summary>
    /// Creates a result indicating trading is allowed
    /// </summary>
    public static PositionSizeResult Allowed(decimal positionSize, decimal riskAmount, CalculationDetail details)
    {
        return new PositionSizeResult
        {
            CanTrade = true,
            PositionSize = positionSize,
            RiskAmount = riskAmount,
            Reason = "Within risk limits",
            Details = details
        };
    }
}
