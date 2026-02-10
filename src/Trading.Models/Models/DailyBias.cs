using System.Text.Json.Serialization;

namespace Trading.Models;

/// <summary>
/// L1 - D1 日线分析结果（Daily Bias）
/// </summary>
/// <remarks>
/// 用于存储 L1 (D1 战略层) 的分析结果，确定当日交易偏见。
/// AI 分析 D1 日线后返回的趋势方向、关键价位和置信度。
/// </remarks>
public class DailyBias
{
    /// <summary>
    /// 交易方向偏见
    /// </summary>
    /// <remarks>
    /// - Bullish: 看涨，寻找做多机会
    /// - Bearish: 看跌，寻找做空机会
    /// - Neutral: 中性，避免交易
    /// </remarks>
    [JsonPropertyName("Direction")]
    public string Direction { get; set; } = "Neutral";

    /// <summary>
    /// 置信度 (0-100)
    /// </summary>
    /// <remarks>
    /// 表示 AI 对该方向判断的信心程度。
    /// 通常要求 >= 60 才会继续后续分析。
    /// </remarks>
    [JsonPropertyName("Confidence")]
    public double Confidence { get; set; }

    /// <summary>
    /// 支撑位列表
    /// </summary>
    /// <remarks>
    /// 从 D1 日线识别的关键支撑价位，按从低到高排序。
    /// 用于判断做多的入场区域和止损位置。
    /// </remarks>
    [JsonPropertyName("SupportLevels")]
    public List<double> SupportLevels { get; set; } = new();

    /// <summary>
    /// 阻力位列表
    /// </summary>
    /// <remarks>
    /// 从 D1 日线识别的关键阻力价位，按从低到高排序。
    /// 用于判断做空的入场区域和止损位置。
    /// </remarks>
    [JsonPropertyName("ResistanceLevels")]
    public List<double> ResistanceLevels { get; set; } = new();

    /// <summary>
    /// 趋势类型
    /// </summary>
    /// <remarks>
    /// - Strong: 强趋势（连续大阳线或大阴线）
    /// - Weak: 弱趋势（小幅波动，方向不明确）
    /// - Sideways: 横盘（在交易区间内波动）
    /// </remarks>
    [JsonPropertyName("TrendType")]
    public string TrendType { get; set; } = "";

    /// <summary>
    /// AI 分析推理过程
    /// </summary>
    /// <remarks>
    /// AI 给出该判断的理由，基于 Al Brooks 理论。
    /// 例如："Strong bull trend with consecutive bull bars closing near highs."
    /// </remarks>
    [JsonPropertyName("Reasoning")]
    public string Reasoning { get; set; } = "";

    /// <summary>
    /// 分析时间
    /// </summary>
    public DateTime AnalyzedAt { get; set; }

    /// <summary>
    /// 获取支撑位数量
    /// </summary>
    [JsonIgnore]
    public int SupportCount => SupportLevels.Count;

    /// <summary>
    /// 获取阻力位数量
    /// </summary>
    [JsonIgnore]
    public int ResistanceCount => ResistanceLevels.Count;

    /// <summary>
    /// 判断是否是高置信度的看涨偏见
    /// </summary>
    [JsonIgnore]
    public bool IsStrongBullish => Direction == "Bullish" && Confidence >= 70;

    /// <summary>
    /// 判断是否是高置信度的看跌偏见
    /// </summary>
    [JsonIgnore]
    public bool IsStrongBearish => Direction == "Bearish" && Confidence >= 70;

    /// <summary>
    /// 判断置信度是否足够继续后续分析
    /// </summary>
    /// <param name="minConfidence">最小置信度要求（默认 60）</param>
    public bool IsConfident(double minConfidence = 60) => Confidence >= minConfidence;
}
