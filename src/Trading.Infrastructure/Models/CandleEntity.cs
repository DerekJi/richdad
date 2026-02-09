using Azure;
using Azure.Data.Tables;

namespace Trading.Infrastructure.Models;

/// <summary>
/// K线实体 - 存储原始 OHLC 数据
/// </summary>
/// <remarks>
/// PartitionKey: Symbol (如 "XAUUSD", "EURUSD")
/// RowKey: TimeFrame_DateTime (如 "M5_20260208_1015")
/// </remarks>
public class CandleEntity : ITableEntity
{
    /// <summary>
    /// 品种代码
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// 时间周期 (D1, H1, M5 等)
    /// </summary>
    public string TimeFrame { get; set; } = string.Empty;

    /// <summary>
    /// K线时间
    /// </summary>
    public DateTime Time { get; set; }

    /// <summary>
    /// 开盘价 (存储为 double，从 decimal 转换)
    /// </summary>
    public double Open { get; set; }

    /// <summary>
    /// 最高价
    /// </summary>
    public double High { get; set; }

    /// <summary>
    /// 最低价
    /// </summary>
    public double Low { get; set; }

    /// <summary>
    /// 收盘价
    /// </summary>
    public double Close { get; set; }

    /// <summary>
    /// 成交量
    /// </summary>
    public long Volume { get; set; }

    /// <summary>
    /// 点差
    /// </summary>
    public int Spread { get; set; }

    /// <summary>
    /// 是否完整（已收盘的 K 线）
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// 数据源 (如 "OANDA", "TradeLocker")
    /// </summary>
    public string Source { get; set; } = "OANDA";

    // Azure Table Storage 必需字段
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    /// <summary>
    /// 从 Candle 创建实体
    /// </summary>
    public static CandleEntity FromCandle(string symbol, string timeFrame, Trading.Models.Candle candle, bool isComplete = true, string source = "OANDA")
    {
        return new CandleEntity
        {
            Symbol = symbol,
            TimeFrame = timeFrame,
            Time = candle.DateTime,
            Open = (double)candle.Open,
            High = (double)candle.High,
            Low = (double)candle.Low,
            Close = (double)candle.Close,
            Volume = candle.TickVolume,
            Spread = candle.Spread,
            IsComplete = isComplete,
            Source = source,
            PartitionKey = symbol,
            RowKey = $"{timeFrame}_{candle.DateTime:yyyyMMdd_HHmm}"
        };
    }

    /// <summary>
    /// 转换为 Candle 模型
    /// </summary>
    public Trading.Models.Candle ToCandle()
    {
        return new Trading.Models.Candle
        {
            DateTime = Time,
            Open = (decimal)Open,
            High = (decimal)High,
            Low = (decimal)Low,
            Close = (decimal)Close,
            TickVolume = Volume,
            Spread = Spread
        };
    }
}
