using Trading.Core.RiskManagement.Models;

namespace Trading.Core.RiskManagement;

/// <summary>
/// Manages risk limits and position sizing with prop firm rules
/// </summary>
public class RiskManager
{
    private readonly Dictionary<string, PropFirmRules> _propFirmRules;
    private readonly Dictionary<string, InstrumentSpecification> _instrumentSpecs;
    private readonly PositionSizer _positionSizer;

    public RiskManager(
        Dictionary<string, PropFirmRules>? propFirmRules = null,
        Dictionary<string, InstrumentSpecification>? instrumentSpecs = null)
    {
        _propFirmRules = propFirmRules ?? new Dictionary<string, PropFirmRules>();
        _instrumentSpecs = instrumentSpecs ?? new Dictionary<string, InstrumentSpecification>();
        _positionSizer = new PositionSizer();
    }

    /// <summary>
    /// Registers a prop firm rule set
    /// </summary>
    public void RegisterPropFirmRule(PropFirmRules rules)
    {
        _propFirmRules[rules.Name] = rules;
    }

    /// <summary>
    /// Registers an instrument specification
    /// </summary>
    public void RegisterInstrumentSpec(InstrumentSpecification spec)
    {
        _instrumentSpecs[spec.GetKey()] = spec;
    }

    /// <summary>
    /// Calculates position size with full risk management checks
    /// </summary>
    public PositionSizeResult CalculatePosition(
        string symbol,
        string broker,
        decimal entryPrice,
        decimal stopLoss,
        RiskParameters riskParams)
    {
        // Validate parameters
        try
        {
            riskParams.Validate();
        }
        catch (Exception ex)
        {
            return PositionSizeResult.NotAllowed($"Invalid parameters: {ex.Message}");
        }

        // Get instrument specification
        var specKey = $"{broker}:{symbol}";
        if (!_instrumentSpecs.TryGetValue(specKey, out var spec))
        {
            return PositionSizeResult.NotAllowed($"Instrument specification not found for {specKey}");
        }

        // Get or create prop firm rules
        PropFirmRules? propRules = null;
        int timeZoneOffset = 2; // Default UTC+2

        if (!string.IsNullOrEmpty(riskParams.PropFirmRule))
        {
            if (!_propFirmRules.TryGetValue(riskParams.PropFirmRule, out propRules))
            {
                return PositionSizeResult.NotAllowed($"Prop firm rule '{riskParams.PropFirmRule}' not found");
            }
            timeZoneOffset = propRules.ServerTimeZoneOffset;
        }
        else if (riskParams.CustomServerTimeZoneOffset.HasValue)
        {
            timeZoneOffset = riskParams.CustomServerTimeZoneOffset.Value;
        }

        // Check if trading day has changed (reset daily loss if needed)
        var currentTradingDay = TradingDayCalculator.GetCurrentTradingDay(timeZoneOffset);
        var serverTime = TradingDayCalculator.GetServerTime(timeZoneOffset);

        var todayLoss = riskParams.TodayLoss;
        if (TradingDayCalculator.HasTradingDayChanged(riskParams.LastResetDate, timeZoneOffset))
        {
            todayLoss = 0; // Reset daily loss
        }

        // Determine risk limits
        decimal dailyLossLimit;
        decimal totalLossLimit;
        decimal baseBalance;

        if (propRules != null)
        {
            baseBalance = propRules.CalculationBase == CalculationBase.InitialBalance
                ? riskParams.InitialBalance
                : riskParams.AccountBalance;

            dailyLossLimit = propRules.GetMaxDailyLossAmount(baseBalance);
            totalLossLimit = propRules.GetMaxTotalLossAmount(baseBalance);
        }
        else
        {
            baseBalance = riskParams.CustomDailyLossPercent.HasValue
                ? riskParams.InitialBalance
                : riskParams.AccountBalance;

            dailyLossLimit = baseBalance * (riskParams.CustomDailyLossPercent!.Value / 100m);
            totalLossLimit = baseBalance * (riskParams.CustomTotalLossPercent!.Value / 100m);
        }

        // Check daily loss limit
        if (todayLoss >= dailyLossLimit)
        {
            return PositionSizeResult.NotAllowed(
                $"Daily loss limit reached: {todayLoss:F2} / {dailyLossLimit:F2}");
        }

        // Check total loss limit
        if (riskParams.TotalLoss >= totalLossLimit)
        {
            return PositionSizeResult.NotAllowed(
                $"Total loss limit reached: {riskParams.TotalLoss:F2} / {totalLossLimit:F2}");
        }

        // Calculate remaining risk allowances
        var dailyRemainingRisk = dailyLossLimit - todayLoss;
        var totalRemainingRisk = totalLossLimit - riskParams.TotalLoss;

        // Get requested risk per trade
        var requestedRiskPerTrade = riskParams.GetEffectiveRiskPerTrade();

        // Limit risk to remaining allowances
        var allowedRiskForTrade = Math.Min(requestedRiskPerTrade, Math.Min(dailyRemainingRisk, totalRemainingRisk));

        if (allowedRiskForTrade <= 0)
        {
            return PositionSizeResult.NotAllowed("No remaining risk allowance");
        }

        // Calculate position size
        var result = _positionSizer.CalculatePositionSize(spec, entryPrice, stopLoss, allowedRiskForTrade);

        // Add detailed information
        if (result.Details != null)
        {
            result.Details.DailyLossLimit = dailyLossLimit;
            result.Details.DailyLoss = todayLoss;
            result.Details.DailyRemainingRisk = dailyRemainingRisk;
            result.Details.TotalLossLimit = totalLossLimit;
            result.Details.TotalLoss = riskParams.TotalLoss;
            result.Details.TotalRemainingRisk = totalRemainingRisk;
            result.Details.RequestedRiskPerTrade = requestedRiskPerTrade;
            result.Details.AllowedRiskForTrade = allowedRiskForTrade;
            result.Details.TradingDay = currentTradingDay;
            result.Details.ServerTime = serverTime;
        }

        // Update reason if risk was limited
        if (allowedRiskForTrade < requestedRiskPerTrade)
        {
            result.Reason = $"Risk limited to {allowedRiskForTrade:F2} (requested {requestedRiskPerTrade:F2})";
        }

        return result;
    }

    /// <summary>
    /// Gets the current trading day for a prop firm rule
    /// </summary>
    public DateTime GetCurrentTradingDay(string propFirmRuleName)
    {
        if (_propFirmRules.TryGetValue(propFirmRuleName, out var rules))
        {
            return rules.GetCurrentTradingDay();
        }

        return TradingDayCalculator.GetCurrentTradingDay(2); // Default UTC+2
    }
}
