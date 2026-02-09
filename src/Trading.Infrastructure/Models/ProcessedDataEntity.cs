using Azure;
using Azure.Data.Tables;
using System.Text.Json;

namespace Trading.Infrastructure.Models;

/// <summary>
/// Al Brooks 形态识别预处理数据实体
/// </summary>
/// <remarks>
/// 存储每根 K 线的技术指标和形态标签，用于：
/// 1. 减少 AI 分析时的计算负担
/// 2. 提供 100% 准确的形态识别结果
/// 3. 支持历史数据回测和分析
///
/// 存储结构：
/// - PartitionKey: "{Symbol}_{TimeFrame}" (如 "XAUUSD_M5")
/// - RowKey: "yyyyMMdd_HHmm" (如 "20260209_1550")
/// - Tags: JSON 数组，包含所有识别到的形态标签
/// </remarks>
public class ProcessedDataEntity : ITableEntity
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
    /// Body% - 收盘位置 (0.0-1.0)
    /// </summary>
    public double BodyPercent { get; set; }

    /// <summary>
    /// Close Position - 同 BodyPercent
    /// </summary>
    public double ClosePosition { get; set; }

    /// <summary>
    /// 与 EMA20 的距离（Ticks）
    /// </summary>
    public double DistanceToEMA20 { get; set; }

    /// <summary>
    /// K线范围 (High - Low)
    /// </summary>
    public double Range { get; set; }

    /// <summary>
    /// 实体大小百分比
    /// </summary>
    public double BodySizePercent { get; set; }

    /// <summary>
    /// 上影线百分比
    /// </summary>
    public double UpperTailPercent { get; set; }

    /// <summary>
    /// 下影线百分比
    /// </summary>
    public double LowerTailPercent { get; set; }

    /// <summary>
    /// EMA20 值
    /// </summary>
    public double EMA20 { get; set; }

    /// <summary>
    /// ATR 值（如果计算）
    /// </summary>
    public double? ATR { get; set; }

    /// <summary>
    /// 形态标签（JSON 数组字符串）
    /// 如: ["Inside", "ii", "Test_EMA20", "H2"]
    /// </summary>
    public string Tags { get; set; } = "[]";

    /// <summary>
    /// 是否为信号棒
    /// </summary>
    public bool IsSignalBar { get; set; }

    /// <summary>
    /// 原始 OHLC 数据（可选，用于快速查询）
    /// </summary>
    public double Open { get; set; }
    public double High { get; set; }
    public double Low { get; set; }
    public double Close { get; set; }
    public long Volume { get; set; }

    // Azure Table Storage 必需字段
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    /// <summary>
    /// 从处理数据创建实体
    /// </summary>
    public static ProcessedDataEntity Create(
        string symbol,
        string timeFrame,
        DateTime time,
        double bodyPercent,
        double distanceToEMA,
        double range,
        double bodySizePercent,
        double upperTailPercent,
        double lowerTailPercent,
        double ema20,
        List<string> tags,
        bool isSignalBar,
        decimal open,
        decimal high,
        decimal low,
        decimal close,
        long volume,
        double? atr = null)
    {
        // 确保时间是 UTC
        var utcTime = time.Kind == DateTimeKind.Utc
            ? time
            : DateTime.SpecifyKind(time, DateTimeKind.Utc);

        return new ProcessedDataEntity
        {
            Symbol = symbol,
            TimeFrame = timeFrame,
            Time = utcTime,
            BodyPercent = bodyPercent,
            ClosePosition = bodyPercent, // 同 BodyPercent
            DistanceToEMA20 = distanceToEMA,
            Range = range,
            BodySizePercent = bodySizePercent,
            UpperTailPercent = upperTailPercent,
            LowerTailPercent = lowerTailPercent,
            EMA20 = ema20,
            ATR = atr,
            Tags = JsonSerializer.Serialize(tags),
            IsSignalBar = isSignalBar,
            Open = (double)open,
            High = (double)high,
            Low = (double)low,
            Close = (double)close,
            Volume = volume,
            PartitionKey = $"{symbol}_{timeFrame}",
            RowKey = utcTime.ToString("yyyyMMdd_HHmm")
        };
    }

    /// <summary>
    /// 获取标签列表
    /// </summary>
    public List<string> GetTags()
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(Tags) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// 转换为字典（用于 API 返回）
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            ["symbol"] = Symbol,
            ["timeFrame"] = TimeFrame,
            ["time"] = Time,
            ["bodyPercent"] = BodyPercent,
            ["closePosition"] = ClosePosition,
            ["distanceToEMA20"] = DistanceToEMA20,
            ["range"] = Range,
            ["bodySizePercent"] = BodySizePercent,
            ["upperTailPercent"] = UpperTailPercent,
            ["lowerTailPercent"] = LowerTailPercent,
            ["ema20"] = EMA20,
            ["atr"] = ATR ?? 0,
            ["tags"] = GetTags(),
            ["isSignalBar"] = IsSignalBar,
            ["ohlc"] = new
            {
                open = Open,
                high = High,
                low = Low,
                close = Close,
                volume = Volume
            }
        };
    }
}
