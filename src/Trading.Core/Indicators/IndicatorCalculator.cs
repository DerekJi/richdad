using Trading.Data.Models;
using Skender.Stock.Indicators;

namespace Trading.Core.Indicators;

/// <summary>
/// 技术指标计算服务 (使用Skender.Stock.Indicators)
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
    /// 计算ATR (Average True Range) 使用Skender.Stock.Indicators
    /// </summary>
    private void CalculateATR(List<Candle> candles, int period)
    {
        if (candles.Count < period) return;
        
        // 转换为Skender Quote格式
        var quotes = candles.Select(c => new Quote
        {
            Date = c.DateTime,
            Open = c.Open,
            High = c.High,
            Low = c.Low,
            Close = c.Close,
            Volume = c.TickVolume
        }).ToList();
        
        // 调用Skender计算ATR
        var atrResults = quotes.GetAtr(period).ToList();
        
        // 填充ATR值到原始candles
        for (int i = 0; i < candles.Count; i++)
        {
            candles[i].ATR = (decimal)(atrResults[i].Atr ?? 0);
        }
    }
    
    /// <summary>
    /// 计算EMA (Exponential Moving Average) 使用Skender.Stock.Indicators
    /// </summary>
    private void CalculateEMA(List<Candle> candles, int period)
    {
        if (candles.Count < period) return;
        
        // 转换为Skender Quote格式
        var quotes = candles.Select(c => new Quote
        {
            Date = c.DateTime,
            Open = c.Open,
            High = c.High,
            Low = c.Low,
            Close = c.Close,
            Volume = c.TickVolume
        }).ToList();
        
        // 调用Skender计算EMA
        var emaResults = quotes.GetEma(period).ToList();
        
        // 填充EMA值到原始candles
        for (int i = 0; i < candles.Count; i++)
        {
            candles[i].EMA[period] = (decimal)(emaResults[i].Ema ?? 0);
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
