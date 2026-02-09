namespace Trading.Services.Extensions;

/// <summary>
/// Candle类型转换扩展方法
/// </summary>
public static class CandleExtensions
{
    /// <summary>
    /// 将 Infrastructure.Services.Candle 转换为 Models.Candle
    /// </summary>
    public static Trading.Models.Candle ToModelCandle(this Trading.Infrastructure.Services.Candle candle)
    {
        return new Trading.Models.Candle
        {
            DateTime = candle.Time,
            Open = candle.Open,
            High = candle.High,
            Low = candle.Low,
            Close = candle.Close,
            TickVolume = (long)candle.Volume,
            Spread = 0 // Infrastructure.Services.Candle 没有 Spread 信息
        };
    }

    /// <summary>
    /// 批量转换 Infrastructure.Services.Candle 列表为 Models.Candle 列表
    /// </summary>
    public static List<Trading.Models.Candle> ToModelCandles(this List<Trading.Infrastructure.Services.Candle> candles)
    {
        return candles.Select(c => c.ToModelCandle()).ToList();
    }

    /// <summary>
    /// 将 Models.Candle 转换为 Infrastructure.Services.Candle
    /// </summary>
    public static Trading.Infrastructure.Services.Candle ToServiceCandle(this Trading.Models.Candle candle)
    {
        return new Trading.Infrastructure.Services.Candle
        {
            Time = candle.DateTime,
            Open = candle.Open,
            High = candle.High,
            Low = candle.Low,
            Close = candle.Close,
            Volume = candle.TickVolume
        };
    }

    /// <summary>
    /// 批量转换 Models.Candle 列表为 Infrastructure.Services.Candle 列表
    /// </summary>
    public static List<Trading.Infrastructure.Services.Candle> ToServiceCandles(this List<Trading.Models.Candle> candles)
    {
        return candles.Select(c => c.ToServiceCandle()).ToList();
    }
}
