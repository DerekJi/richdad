using Trading.Core.RiskManagement.Models;

namespace Trading.Core.RiskManagement.Configuration;

/// <summary>
/// Configuration settings for instrument specifications
/// </summary>
public class InstrumentSpecificationSettings
{
    public Dictionary<string, BrokerInstruments> Brokers { get; set; } = new();
}

/// <summary>
/// Instruments grouped by broker
/// </summary>
public class BrokerInstruments
{
    public Dictionary<string, InstrumentConfig> Instruments { get; set; } = new();
}

/// <summary>
/// Individual instrument configuration
/// </summary>
public class InstrumentConfig
{
    public string Symbol { get; set; } = string.Empty;
    public decimal ContractSize { get; set; }
    public decimal TickSize { get; set; }
    public decimal TickValue { get; set; }
    public int Leverage { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Converts configuration to InstrumentSpecification model
    /// </summary>
    public InstrumentSpecification ToModel(string broker)
    {
        return new InstrumentSpecification
        {
            Symbol = Symbol,
            Broker = broker,
            ContractSize = ContractSize,
            TickSize = TickSize,
            TickValue = TickValue,
            Leverage = Leverage,
            IsActive = IsActive
        };
    }
}
