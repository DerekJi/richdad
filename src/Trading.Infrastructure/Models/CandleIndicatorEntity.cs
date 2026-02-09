using Azure;
using Azure.Data.Tables;

namespace Trading.Infrastructure.Models;

/// <summary>
/// K线指标实体 - 存储计算后的技术指标
/// </summary>
/// <remarks>
/// PartitionKey: Symbol_TimeFrame (如 "XAUUSD_M5")
/// RowKey: DateTime (如 "20260208_1015")
/// </remarks>
public class CandleIndicatorEntity : ITableEntity
{
    /// <summary>
    /// 品种代码
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// 时间周期
    /// </summary>
    public string TimeFrame { get; set; } = string.Empty;

    /// <summary>
    /// K线时间
    /// </summary>
    public DateTime Time { get; set; }

    /// <summary>
    /// Al Brooks 核心指标 - K线实体位置百分比
    /// </summary>
    /// <remarks>
    /// 计算公式: (Close - Low) / (High - Low)
    /// 范围: 0-1, 接近1表示收盘在顶部，接近0表示收盘在底部
    /// </remarks>
    public double BodyPercent { get; set; }

    /// <summary>
    /// 收盘位置（同 BodyPercent）
    /// </summary>
    public double ClosePosition { get; set; }

    /// <summary>
    /// 距离 EMA20 的偏离度
    /// </summary>
    /// <remarks>
    /// 计算公式: Close - EMA20
    /// 正值表示价格在均线上方，负值表示在下方
    /// </remarks>
    public double DistanceToEMA20 { get; set; }

    /// <summary>
    /// K线范围（高低差）
    /// </summary>
    public double Range { get; set; }

    /// <summary>
    /// EMA20 指标值
    /// </summary>
    public double EMA20 { get; set; }

    /// <summary>
    /// ATR 指标值（Average True Range）
    /// </summary>
    public double ATR { get; set; }

    /// <summary>
    /// 形态标签（JSON 数组字符串）
    /// </summary>
    /// <remarks>
    /// 示例: ["ii", "H2", "Signal"]
    /// ii: inside bar (内包K线)
    /// H2: High 2 (两根K线高点)
    /// Signal: 信号K线
    /// </remarks>
    public string Tags { get; set; } = "[]";

    // Azure Table Storage 必需字段
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    /// <summary>
    /// 创建实体（手动设置各项指标）
    /// </summary>
    public static CandleIndicatorEntity Create(
        string symbol,
        string timeFrame,
        DateTime time,
        double bodyPercent,
        double distanceToEMA20,
        double range,
        double ema20,
        double atr,
        string[] tags)
    {
        return new CandleIndicatorEntity
        {
            Symbol = symbol,
            TimeFrame = timeFrame,
            Time = time,
            BodyPercent = bodyPercent,
            ClosePosition = bodyPercent, // 等同于 BodyPercent
            DistanceToEMA20 = distanceToEMA20,
            Range = range,
            EMA20 = ema20,
            ATR = atr,
            Tags = System.Text.Json.JsonSerializer.Serialize(tags),
            PartitionKey = $"{symbol}_{timeFrame}",
            RowKey = $"{time:yyyyMMdd_HHmm}"
        };
    }

    /// <summary>
    /// 从 Candle 计算并创建实体
    /// </summary>
    public static CandleIndicatorEntity FromCandle(
        string symbol,
        string timeFrame,
        Trading.Models.Candle candle,
        decimal ema20Value,
        string[] tags)
    {
        var range = (double)(candle.High - candle.Low);
        var bodyPercent = range > 0 ? (double)((candle.Close - candle.Low) / (candle.High - candle.Low)) : 0.5;
        var distanceToEMA20 = (double)(candle.Close - ema20Value);

        return new CandleIndicatorEntity
        {
            Symbol = symbol,
            TimeFrame = timeFrame,
            Time = candle.DateTime,
            BodyPercent = bodyPercent,
            ClosePosition = bodyPercent,
            DistanceToEMA20 = distanceToEMA20,
            Range = range,
            EMA20 = (double)ema20Value,
            ATR = (double)candle.ATR,
            Tags = System.Text.Json.JsonSerializer.Serialize(tags),
            PartitionKey = $"{symbol}_{timeFrame}",
            RowKey = $"{candle.DateTime:yyyyMMdd_HHmm}"
        };
    }

    /// <summary>
    /// 获取标签数组
    /// </summary>
    public string[] GetTags()
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<string[]>(Tags) ?? Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}
