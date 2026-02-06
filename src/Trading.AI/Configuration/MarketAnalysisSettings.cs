namespace Trading.AI.Configuration;

/// <summary>
/// 市场分析配置
/// </summary>
public class MarketAnalysisSettings
{
    public const string SectionName = "MarketAnalysis";

    /// <summary>
    /// 趋势分析缓存时长（分钟）
    /// </summary>
    public int TrendAnalysisCacheMinutes { get; set; } = 360; // 6小时

    /// <summary>
    /// 关键价格位缓存时长（分钟）
    /// </summary>
    public int KeyLevelsCacheMinutes { get; set; } = 720; // 12小时

    /// <summary>
    /// 用于趋势分析的K线数量
    /// </summary>
    public int TrendAnalysisCandles { get; set; } = 100;

    /// <summary>
    /// 用于支撑阻力位识别的K线数量
    /// </summary>
    public int KeyLevelsCandles { get; set; } = 200;

    /// <summary>
    /// 是否启用详细日志
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// 最小信号质量分数（0-100）
    /// </summary>
    public int MinSignalQualityScore { get; set; } = 60;
}
