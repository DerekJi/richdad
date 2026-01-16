using Trading.Core.Indicators;
using Trading.Data.Models;

namespace Trading.Core.Strategies;

/// <summary>
/// Pin Bar交易策略
/// </summary>
public class PinBarStrategy : ITradingStrategy
{
    private readonly StrategyConfig _config;

    public string Name => _config.StrategyName;

    public PinBarStrategy(StrategyConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// 检查是否可以开多单
    /// 逻辑: 
    /// 1. 当前无持仓
    /// 2. 前一根K线是看涨Pin Bar
    /// 3. 前一根K线收盘在EMA200上方
    /// 4. 前一根K线靠近某个EMA
    /// 5. 当前K线收盘高于前一根K线的高点 (突破确认)
    /// 6. 在交易时间内
    /// </summary>
    public bool CanOpenLong(Candle current, Candle previous, bool hasOpenPosition)
    {
        if (hasOpenPosition) return false;
        if (!IsValidTradingTime(current)) return false;
        
        var baseEma = IndicatorCalculator.GetEma(previous, _config.BaseEma);
        if (baseEma == 0) return false; // EMA未准备好
        
        // 前一根K线收盘必须在EMA200上方
        if (previous.Close <= baseEma) return false;
        
        // 前一根K线必须是看涨Pin Bar
        if (!IsPinBar(previous, bullish: true)) return false;
        
        // 前一根K线必须靠近某个EMA
        if (!NearAnyEma(previous, bullish: true)) return false;
        
        // 当前K线收盘必须高于前一根K线的高点 (突破确认)
        if (current.Close <= previous.High) return false;
        
        return true;
    }

    /// <summary>
    /// 检查是否可以开空单
    /// </summary>
    public bool CanOpenShort(Candle current, Candle previous, bool hasOpenPosition)
    {
        if (hasOpenPosition) return false;
        if (!IsValidTradingTime(current)) return false;
        
        var baseEma = IndicatorCalculator.GetEma(previous, _config.BaseEma);
        if (baseEma == 0) return false;
        
        // 前一根K线收盘必须在EMA200下方
        if (previous.Close >= baseEma) return false;
        
        // 前一根K线必须是看跌Pin Bar
        if (!IsPinBar(previous, bullish: false)) return false;
        
        // 前一根K线必须靠近某个EMA
        if (!NearAnyEma(previous, bullish: false)) return false;
        
        // 当前K线收盘必须低于前一根K线的低点 (突破确认)
        if (current.Close >= previous.Low) return false;
        
        return true;
    }

    /// <summary>
    /// 判断是否为Pin Bar
    /// </summary>
    private bool IsPinBar(Candle candle, bool bullish)
    {
        var total = candle.TotalRange;
        
        // 过滤波动太小的K线
        if (total < _config.Threshold) return false;
        
        // 计算长影线和短影线
        var longerWick = bullish ? candle.LowerWick : candle.UpperWick;
        var shorterWick = bullish ? candle.UpperWick : candle.LowerWick;
        var body = candle.BodySize;
        
        // 长影线必须足够长 (至少是ATR的1.2倍)
        if (candle.ATR > 0 && longerWick < _config.MinLowerWickAtrRatio * candle.ATR)
        {
            return false;
        }
        
        // 实体占比不能太大
        if (total > 0 && body / total * 100 > _config.MaxBodyPercentage)
        {
            return false;
        }
        
        // 长影线占比必须足够大
        if (total > 0 && longerWick / total * 100 < _config.MinLongerWickPercentage)
        {
            return false;
        }
        
        // 短影线占比不能太大
        if (total > 0 && shorterWick / total * 100 > _config.MaxShorterWickPercentage)
        {
            return false;
        }
        
        // 如果要求Pin Bar方向匹配
        if (_config.RequirePinBarDirectionMatch)
        {
            if (bullish && candle.IsBearish) return false;
            if (!bullish && candle.IsBullish) return false;
        }
        
        return true;
    }

    /// <summary>
    /// 判断是否靠近任意一个EMA
    /// </summary>
    private bool NearAnyEma(Candle candle, bool bullish)
    {
        foreach (var emaPeriod in _config.EmaList)
        {
            var emaValue = IndicatorCalculator.GetEma(candle, emaPeriod);
            if (emaValue == 0) continue; // EMA未准备好
            
            if (bullish)
            {
                // 多单: 实体在EMA上方，且低点靠近或穿过EMA
                var minBody = Math.Min(candle.Open, candle.Close);
                if (minBody > emaValue && 
                    (Math.Abs(candle.Low - emaValue) <= _config.NearEmaThreshold || candle.Low < emaValue))
                {
                    return true;
                }
            }
            else
            {
                // 空单: 实体在EMA下方，且高点靠近或穿过EMA
                var maxBody = Math.Max(candle.Open, candle.Close);
                if (maxBody < emaValue && 
                    (Math.Abs(candle.High - emaValue) <= _config.NearEmaThreshold || candle.High > emaValue))
                {
                    return true;
                }
            }
        }
        
        return false;
    }

    /// <summary>
    /// 检查是否在有效交易时间内
    /// </summary>
    private bool IsValidTradingTime(Candle candle)
    {
        var hour = candle.UtcHour;
        
        // 首先检查是否在禁止开单时间
        if (_config.NoTradeHours != null && _config.NoTradeHours.Contains(hour))
            return false;
        
        // 然后检查交易时段限制
        if (_config.NoTradingHoursLimit)
            return true;
            
        return hour >= _config.StartTradingHour && hour <= _config.EndTradingHour;
    }

    /// <summary>
    /// 计算止损位
    /// </summary>
    public decimal CalculateStopLoss(Candle pinbar, TradeDirection direction)
    {
        switch (_config.StopLossStrategy)
        {
            case StopLossStrategy.PinbarEndPlusAtr:
                var offset = _config.StopLossAtrRatio * pinbar.ATR;
                return direction == TradeDirection.Long
                    ? pinbar.Low - offset
                    : pinbar.High + offset;
            
            default:
                throw new NotImplementedException($"止损策略 {_config.StopLossStrategy} 未实现");
        }
    }

    /// <summary>
    /// 计算止盈位
    /// </summary>
    public decimal CalculateTakeProfit(decimal entryPrice, decimal stopLoss, TradeDirection direction)
    {
        var risk = Math.Abs(entryPrice - stopLoss);
        var reward = risk * _config.RiskRewardRatio;
        
        return direction == TradeDirection.Long
            ? entryPrice + reward
            : entryPrice - reward;
    }
}
