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
        if (config.AtrPeriod > 0)
        {
            CalculateATR(candles, config.AtrPeriod);
        }

        // 计算ADX (支持多周期)
        if (config.AdxPeriod > 0)
        {
            if (config.AdxTimeframe == AdxTimeframe.Current)
            {
                // 使用当前周期计算ADX
                CalculateADX(candles, config.AdxPeriod);
            }
            else
            {
                // 使用更高周期计算ADX
                CalculateHigherTimeframeADX(candles, config.AdxPeriod, config.AdxTimeframe);
            }
        }

        // 计算所有EMA
        foreach (var period in config.EmaList.Where(p => p > 0))
        {
            CalculateEMA(candles, period);
        }

        // 确保基准EMA也被计算
        if (config.BaseEma > 0 && !config.EmaList.Contains(config.BaseEma))
        {
            CalculateEMA(candles, config.BaseEma);
        }
    }

    /// <summary>
    /// 计算ATR (Average True Range) 使用Skender.Stock.Indicators
    /// </summary>
    private void CalculateATR(List<Candle> candles, int period)
    {
        if (candles.Count < period || period <= 0) return;

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
        if (candles.Count < period || period <= 0) return;

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
    /// 计算ADX (Average Directional Index) 使用Skender.Stock.Indicators
    /// </summary>
    private void CalculateADX(List<Candle> candles, int period)
    {
        if (candles.Count < period * 2 || period <= 0) return;

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

        // 调用Skender计算ADX
        var adxResults = quotes.GetAdx(period).ToList();

        // 填充ADX值到原始candles
        for (int i = 0; i < candles.Count; i++)
        {
            candles[i].ADX = (decimal)(adxResults[i].Adx ?? 0);
        }
    }

    /// <summary>
    /// 计算更高时间周期的ADX并映射到当前周期
    /// </summary>
    private void CalculateHigherTimeframeADX(List<Candle> candles, int period, AdxTimeframe timeframe)
    {
        if (candles.Count < period * 2 || period <= 0) return;

        // 获取时间周期的分钟数
        int minutes = timeframe switch
        {
            AdxTimeframe.H1 => 60,
            AdxTimeframe.H4 => 240,
            AdxTimeframe.Daily => 1440,
            _ => 15 // Current (M15)
        };

        // 将M15 K线聚合为更高周期
        var higherTimeframeCandles = AggregateToHigherTimeframe(candles, minutes);

        if (higherTimeframeCandles.Count < period * 2) return;

        // 转换为Skender Quote格式
        var quotes = higherTimeframeCandles.Select(c => new Quote
        {
            Date = c.DateTime,
            Open = c.Open,
            High = c.High,
            Low = c.Low,
            Close = c.Close,
            Volume = c.TickVolume
        }).ToList();

        // 调用Skender计算ADX
        var adxResults = quotes.GetAdx(period).ToList();

        // 填充ADX值到高周期K线
        for (int i = 0; i < higherTimeframeCandles.Count; i++)
        {
            higherTimeframeCandles[i].ADX = (decimal)(adxResults[i].Adx ?? 0);
        }

        // 将高周期ADX映射到低周期K线
        MapHigherTimeframeADXToLowerTimeframe(candles, higherTimeframeCandles);
    }

    /// <summary>
    /// 将M15 K线聚合为更高时间周期
    /// </summary>
    private List<Candle> AggregateToHigherTimeframe(List<Candle> candles, int targetMinutes)
    {
        var result = new List<Candle>();
        var currentBar = new List<Candle>();
        DateTime? currentPeriodStart = null;

        foreach (var candle in candles)
        {
            // 计算当前K线所属的时间周期起始时间
            var periodStart = GetPeriodStart(candle.DateTime, targetMinutes);

            // 如果是新的周期，保存之前的聚合K线
            if (currentPeriodStart.HasValue && periodStart != currentPeriodStart.Value && currentBar.Count > 0)
            {
                result.Add(AggregateCandles(currentBar, currentPeriodStart.Value));
                currentBar.Clear();
            }

            currentPeriodStart = periodStart;
            currentBar.Add(candle);
        }

        // 添加最后一个周期
        if (currentBar.Count > 0 && currentPeriodStart.HasValue)
        {
            result.Add(AggregateCandles(currentBar, currentPeriodStart.Value));
        }

        return result;
    }

    /// <summary>
    /// 获取时间周期起始时间
    /// 例如：H1时，22:15 → 22:00；H4时，01:00 → 00:00
    /// </summary>
    private DateTime GetPeriodStart(DateTime dateTime, int minutes)
    {
        if (minutes == 1440) // Daily
        {
            return dateTime.Date;
        }
        else
        {
            // 计算从0点开始经过的分钟数
            var totalMinutes = (int)dateTime.TimeOfDay.TotalMinutes;
            // 整数除法得到周期索引（例如：H1时，75/60=1表示第1个小时）
            var periodIndex = totalMinutes / minutes;
            // 返回该周期的起始时间
            return dateTime.Date.AddMinutes(periodIndex * minutes);
        }
    }

    /// <summary>
    /// 聚合多根K线为一根
    /// 例如：4根M15 K线(22:00, 22:15, 22:30, 22:45) → 1根H1 K线(22:00)
    /// Open=第一根的Open, High=最高的High, Low=最低的Low, Close=最后的Close, Volume=累加
    /// </summary>
    private Candle AggregateCandles(List<Candle> candles, DateTime periodStart)
    {
        return new Candle
        {
            DateTime = periodStart,
            Open = candles.First().Open,
            High = candles.Max(c => c.High),
            Low = candles.Min(c => c.Low),
            Close = candles.Last().Close,
            TickVolume = candles.Sum(c => c.TickVolume)
        };
    }

    /// <summary>
    /// 将高周期ADX映射到低周期K线
    /// 每个低周期K线会被赋予其所属高周期K线的ADX值
    /// 例如：H1的22:00 ADX=30 → M15的22:00,22:15,22:30,22:45都设为30
    /// </summary>
    private void MapHigherTimeframeADXToLowerTimeframe(List<Candle> lowerTimeframeCandles, List<Candle> higherTimeframeCandles)
    {
        int higherIndex = 0;

        foreach (var candle in lowerTimeframeCandles)
        {
            // 找到对应的高周期K线：找到最后一个起始时间 <= 当前K线时间的高周期K线
            while (higherIndex < higherTimeframeCandles.Count - 1 &&
                   candle.DateTime >= higherTimeframeCandles[higherIndex + 1].DateTime)
            {
                higherIndex++;
            }

            if (higherIndex < higherTimeframeCandles.Count)
            {
                candle.ADX = higherTimeframeCandles[higherIndex].ADX;
            }
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
