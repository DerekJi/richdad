namespace Trading.Services.Models;

/// <summary>
/// EMA监测状态
/// </summary>
public class EmaMonitoringState
{
    /// <summary>
    /// 唯一标识：品种_周期_EMA周期
    /// </summary>
    public string Id => $"{Symbol}_{TimeFrame}_EMA{EmaPeriod}";

    /// <summary>
    /// 品种
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// K线周期
    /// </summary>
    public string TimeFrame { get; set; } = string.Empty;

    /// <summary>
    /// EMA周期
    /// </summary>
    public int EmaPeriod { get; set; }

    /// <summary>
    /// 上一根K线收盘价
    /// </summary>
    public decimal LastClose { get; set; }

    /// <summary>
    /// 上一根K线的EMA值
    /// </summary>
    public decimal LastEmaValue { get; set; }

    /// <summary>
    /// 上一根K线价格相对于EMA的位置 (Above=1, Below=-1)
    /// </summary>
    public int LastPosition { get; set; }

    /// <summary>
    /// 上一根K线的时间戳（用于避免重复处理同一根K线）
    /// </summary>
    public DateTime LastCandleTime { get; set; }

    /// <summary>
    /// 最后检查时间
    /// </summary>
    public DateTime LastCheckTime { get; set; }

    /// <summary>
    /// 最后通知时间
    /// </summary>
    public DateTime? LastNotificationTime { get; set; }
}

/// <summary>
/// EMA穿越事件
/// </summary>
public class EmaCrossEvent
{
    /// <summary>
    /// 品种
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// K线周期
    /// </summary>
    public string TimeFrame { get; set; } = string.Empty;

    /// <summary>
    /// EMA周期
    /// </summary>
    public int EmaPeriod { get; set; }

    /// <summary>
    /// 当前K线收盘价
    /// </summary>
    public decimal CurrentClose { get; set; }

    /// <summary>
    /// 当前EMA值
    /// </summary>
    public decimal CurrentEmaValue { get; set; }

    /// <summary>
    /// 穿越类型 (CrossAbove=上穿, CrossBelow=下穿)
    /// </summary>
    public CrossType CrossType { get; set; }

    /// <summary>
    /// 发生时间
    /// </summary>
    public DateTime EventTime { get; set; }

    /// <summary>
    /// 格式化通知消息
    /// </summary>
    public string FormatMessage()
    {
        var crossText = CrossType == CrossType.CrossAbove ? "上穿" : "下穿";
        return $"🔔 EMA穿越提醒\n\n" +
               $"品种: {Symbol}\n" +
               $"周期: {TimeFrame}\n" +
               $"事件: K线收盘价 {crossText} EMA{EmaPeriod}\n" +
               $"收盘价: {CurrentClose:F4}\n" +
               $"EMA{EmaPeriod}: {CurrentEmaValue:F4}\n" +
               $"时间: {EventTime:yyyy-MM-dd HH:mm:ss}";
    }
}

/// <summary>
/// 穿越类型
/// </summary>
public enum CrossType
{
    /// <summary>
    /// 上穿
    /// </summary>
    CrossAbove,

    /// <summary>
    /// 下穿
    /// </summary>
    CrossBelow
}
