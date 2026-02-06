using Trading.Core.RiskManagement.Models;

namespace Trading.Core.RiskManagement;

/// <summary>
/// Core position sizing calculator
/// </summary>
public class PositionSizer
{
    /// <summary>
    /// Calculates the position size based on risk parameters and instrument specification
    /// </summary>
    /// <param name="spec">Instrument specification</param>
    /// <param name="entryPrice">Entry price for the trade</param>
    /// <param name="stopLoss">Stop loss price</param>
    /// <param name="riskAmount">Risk amount in account currency</param>
    /// <returns>Position size result with details</returns>
    public PositionSizeResult CalculatePositionSize(
        InstrumentSpecification spec,
        decimal entryPrice,
        decimal stopLoss,
        decimal riskAmount)
    {
        if (spec == null)
            throw new ArgumentNullException(nameof(spec));

        if (entryPrice <= 0)
            throw new ArgumentException("Entry price must be greater than 0", nameof(entryPrice));

        if (stopLoss <= 0)
            throw new ArgumentException("Stop loss must be greater than 0", nameof(stopLoss));

        if (riskAmount <= 0)
            throw new ArgumentException("Risk amount must be greater than 0", nameof(riskAmount));

        // Calculate risk in price points
        var priceRisk = Math.Abs(entryPrice - stopLoss);

        if (priceRisk == 0)
            return PositionSizeResult.NotAllowed("Entry price and stop loss cannot be the same");

        // Calculate risk in pips/points
        var pipsRisk = priceRisk / spec.TickSize;

        // Calculate risk per lot
        var perLotRisk = CalculatePerLotRisk(spec, priceRisk);

        if (perLotRisk == 0)
            return PositionSizeResult.NotAllowed("Per lot risk cannot be 0");

        // Calculate position size in lots
        var positionSize = riskAmount / perLotRisk;

        // Round down to 2 decimal places (0.01 lot minimum for most brokers)
        positionSize = Math.Floor(positionSize * 100) / 100;

        if (positionSize < 0.01m)
            return PositionSizeResult.NotAllowed("Calculated position size is too small (< 0.01 lot)");

        var detail = new CalculationDetail
        {
            EntryPrice = entryPrice,
            StopLoss = stopLoss,
            PipsRisk = pipsRisk,
            PerLotRisk = perLotRisk,
            ContractSize = spec.ContractSize,
            TickSize = spec.TickSize,
            RequestedRiskPerTrade = riskAmount,
            AllowedRiskForTrade = riskAmount
        };

        return PositionSizeResult.Allowed(positionSize, riskAmount, detail);
    }

    /// <summary>
    /// Calculates the risk per lot for the given instrument
    /// </summary>
    private decimal CalculatePerLotRisk(InstrumentSpecification spec, decimal priceRisk)
    {
        // Risk per lot = Price Risk × Contract Size
        // For example:
        // - Gold: 5.50 USD × 100 oz = 550 USD per lot
        // - EUR/USD: 0.00100 × 100,000 = 100 USD per lot
        return priceRisk * spec.ContractSize;
    }

    /// <summary>
    /// Calculates the minimum stop loss distance required for a given position size
    /// </summary>
    public decimal CalculateMinStopLossDistance(
        InstrumentSpecification spec,
        decimal positionSize,
        decimal maxRiskAmount)
    {
        if (spec == null)
            throw new ArgumentNullException(nameof(spec));

        if (positionSize <= 0)
            throw new ArgumentException("Position size must be greater than 0", nameof(positionSize));

        if (maxRiskAmount <= 0)
            throw new ArgumentException("Max risk amount must be greater than 0", nameof(maxRiskAmount));

        // Max Risk = Position Size × Per Lot Risk
        // Per Lot Risk = Price Risk × Contract Size
        // Therefore: Price Risk = Max Risk / (Position Size × Contract Size)
        var priceRisk = maxRiskAmount / (positionSize * spec.ContractSize);

        return priceRisk;
    }

    /// <summary>
    /// Calculates the maximum position size for a given stop loss distance
    /// </summary>
    public decimal CalculateMaxPositionSize(
        InstrumentSpecification spec,
        decimal stopLossDistance,
        decimal maxRiskAmount)
    {
        if (spec == null)
            throw new ArgumentNullException(nameof(spec));

        if (stopLossDistance <= 0)
            throw new ArgumentException("Stop loss distance must be greater than 0", nameof(stopLossDistance));

        if (maxRiskAmount <= 0)
            throw new ArgumentException("Max risk amount must be greater than 0", nameof(maxRiskAmount));

        var perLotRisk = stopLossDistance * spec.ContractSize;
        var positionSize = maxRiskAmount / perLotRisk;

        // Round down to 2 decimal places
        return Math.Floor(positionSize * 100) / 100;
    }
}
