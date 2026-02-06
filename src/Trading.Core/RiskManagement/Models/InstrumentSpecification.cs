namespace Trading.Core.RiskManagement.Models;

/// <summary>
/// Represents the specification of a trading instrument for a specific broker/platform
/// </summary>
public class InstrumentSpecification
{
    /// <summary>
    /// Symbol name (e.g., XAUUSD, EURUSD, XAGUSD)
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Broker/Platform name (e.g., IC Markets, OANDA)
    /// </summary>
    public string Broker { get; set; } = string.Empty;

    /// <summary>
    /// Contract size (e.g., 100 for gold, 100000 for forex pairs)
    /// </summary>
    public decimal ContractSize { get; set; }

    /// <summary>
    /// Minimum price movement (e.g., 0.01 for gold, 0.00001 for EUR/USD)
    /// </summary>
    public decimal TickSize { get; set; }

    /// <summary>
    /// Value per tick in account currency
    /// </summary>
    public decimal TickValue { get; set; }

    /// <summary>
    /// Leverage ratio (e.g., 500, 100, 30)
    /// </summary>
    public int Leverage { get; set; }

    /// <summary>
    /// Whether this specification is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Calculates the value per pip/point for this instrument
    /// </summary>
    public decimal CalculatePointValue()
    {
        return ContractSize * TickSize;
    }

    /// <summary>
    /// Creates a unique key for this specification
    /// </summary>
    public string GetKey()
    {
        return $"{Broker}:{Symbol}";
    }
}
