namespace Trading.Infrastructure.Configuration;

/// <summary>
/// K线缓存配置
/// </summary>
public class CandleCacheSettings
{
    public const string SectionName = "CandleCache";

    /// <summary>
    /// 是否启用智能缓存
    /// </summary>
    public bool EnableSmartCache { get; set; } = true;

    /// <summary>
    /// 缓存最大保留天数
    /// </summary>
    public int MaxCacheAgeDays { get; set; } = 90;

    /// <summary>
    /// 是否启用自动刷新
    /// </summary>
    public bool AutoRefreshEnabled { get; set; } = true;

    /// <summary>
    /// 自动刷新间隔（分钟）
    /// </summary>
    public int RefreshIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// 预加载的品种列表
    /// </summary>
    public List<string> PreloadSymbols { get; set; } = new()
    {
        "XAUUSD",
        "XAGUSD",
        "EURUSD",
        "AUDUSD",
        "USDJPY"
    };

    /// <summary>
    /// 预加载的时间周期
    /// </summary>
    public List<string> PreloadTimeFrames { get; set; } = new()
    {
        "M5",
        "M15",
        "H1",
        "H4",
        "D1"
    };

    /// <summary>
    /// 预加载的K线数量
    /// </summary>
    public int PreloadCandleCount { get; set; } = 500;
}
