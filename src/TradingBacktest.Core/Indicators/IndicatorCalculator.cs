using TradingBacktest.Data.Models;

namespace TradingBacktest.Core.Indicators;

/// <summary>
/// 技术指标计算服务 (手动实现)
/// </summary>
public class IndicatorCalculator
{
    /// <summary>
    /// 为K线数据计算所有技术指标
    /// </summary>
    public void CalculateIndicators(List<Candle> candles, StrategyConfig config)
    {
        if (candles.Count == 0) return;
        
        // 计算ATR
        CalculateATR(candles, config.AtrPeriod);
        
        // 计算所有EMA
        foreach (var period in config.EmaList)
        {
            CalculateEMA(candles, period);
        }
        
        // 确保基准EMA也被计算
        if (!config.EmaList.Contains(config.BaseEma))
        {
            CalculateEMA(candles, config.BaseEma);
        }
    }
    
    /// <summary>
    /// 计算ATR (Average True Range)
    /// </summary>
    private void CalculateATR(List<Candle> candles, int period)
    {
        if (candles.Count < period) return;
        
        // 计算True Range
        for (int i = 0; i < candles.Count; i++)
        {
            var candle = candles[i];
            
            if (i == 0)
            {
                candle.ATR = candle.High - candle.Low;
            }
            else
            {
                var prevClose = candles[i - 1].Close;
                var tr1 = candle.High - candle.Low;
                var tr2 = Math.Abs(candle.High - prevClose);
                var tr3 = Math.Abs(candle.Low - prevClose);
                var tr = Math.Max(tr1, Math.Max(tr2, tr3));
                
                // 使用简单移动平均计算ATR (第一个ATR)
                if (i < period)
                {
                    candle.ATR = tr;
                }
                else if (i == period)
                {
                    var sum = candles.Skip(1).Take(period).Sum(c => 
                    {
                        var idx = candles.IndexOf(c);
                        var pc = candles[idx - 1].Close;
                        var t1 = c.High - c.Low;
                        var t2 = Math.Abs(c.High - pc);
                        var t3 = Math.Abs(c.Low - pc);
                        return Math.Max(t1, Math.Max(t2, t3));
                    });
                    candle.ATR = sum / period;
                }
                else
                {
                    // 使用指数移动平均
                    var prevATR = candles[i - 1].ATR;
                    candle.ATR = (prevATR * (period - 1) + tr) / period;
                }
            }
        }
    }
    
    /// <summary>
    /// 计算EMA (Exponential Moving Average)
    /// </summary>
    private void CalculateEMA(List<Candle> candles, int period)
    {
        if (candles.Count < period) return;
        
        decimal multiplier = 2.0m / (period + 1);
        
        // 第一个EMA使用SMA
        var sma = candles.Take(period).Average(c => c.Close);
        candles[period - 1].EMA[period] = sma;
        
        // 后续使用EMA公式: EMA = (Close - EMA(previous)) * multiplier + EMA(previous)
        for (int i = period; i < candles.Count; i++)
        {
            var prevEma = candles[i - 1].EMA.ContainsKey(period) ? candles[i - 1].EMA[period] : sma;
            candles[i].EMA[period] = (candles[i].Close - prevEma) * multiplier + prevEma;
        }
        
        // 填充前面的值为0
        for (int i = 0; i < period - 1; i++)
        {
            candles[i].EMA[period] = 0;
        }
    }
    
    /// <summary>
    /// 获取指定周期的EMA值
    /// </summary>
    public static decimal GetEma(Candle candle, int period)
    {
        return candle.EMA.ContainsKey(period) ? candle.EMA[period] : 0;
    }
}
