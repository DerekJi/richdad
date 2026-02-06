namespace Trading.AI.Models;

/// <summary>
/// 趋势分析结果
/// </summary>
public class TrendAnalysis
{
    /// <summary>
    /// 时间框架（如 H1, H4, D1）
    /// </summary>
    public string TimeFrame { get; set; } = string.Empty;

    /// <summary>
    /// 趋势方向：Bullish, Bearish, Neutral
    /// </summary>
    public TrendDirection Direction { get; set; }

    /// <summary>
    /// 趋势强度（0-100）
    /// </summary>
    public int Strength { get; set; }

    /// <summary>
    /// 分析时间
    /// </summary>
    public DateTime AnalyzedAt { get; set; }

    /// <summary>
    /// AI分析详情
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// 是否处于趋势中
    /// </summary>
    public bool IsTrending => Strength >= 60;
}

public enum TrendDirection
{
    Bullish,
    Bearish,
    Neutral
}
