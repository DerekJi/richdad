namespace Trading.Services.Configuration;

/// <summary>
/// EMA监测配置
/// </summary>
public class EmaMonitoringSettings
{
    /// <summary>
    /// 是否启用EMA监测
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 监测的品种列表
    /// </summary>
    public List<string> Symbols { get; set; } = new();

    /// <summary>
    /// K线周期列表 (例如: M5, M15, H1, H4, D1)
    /// </summary>
    public List<string> TimeFrames { get; set; } = new();

    /// <summary>
    /// EMA周期列表
    /// </summary>
    public List<int> EmaPeriods { get; set; } = new();

    /// <summary>
    /// 计算EMA需要的额外历史K线数量（倍数）
    /// 例如：EMA200需要至少200根K线，设置为2则获取400根
    /// </summary>
    public int HistoryMultiplier { get; set; } = 3;

    /// <summary>
    /// 最小检查间隔（秒）- 由最小K线周期决定
    /// </summary>
    public int MinCheckIntervalSeconds
    {
        get
        {
            if (!TimeFrames.Any()) return 300; // 默认5分钟

            var minTimeFrame = TimeFrames
                .Select(tf => ConvertTimeFrameToSeconds(tf))
                .Min();

            return minTimeFrame;
        }
    }

    /// <summary>
    /// 将时间周期转换为秒数
    /// </summary>
    private int ConvertTimeFrameToSeconds(string timeFrame)
    {
        if (timeFrame.StartsWith("M"))
        {
            return int.Parse(timeFrame.Substring(1)) * 60;
        }
        else if (timeFrame.StartsWith("H"))
        {
            return int.Parse(timeFrame.Substring(1)) * 3600;
        }
        else if (timeFrame.StartsWith("D"))
        {
            return int.Parse(timeFrame.Substring(1)) * 86400;
        }
        return 300; // 默认5分钟
    }
}
