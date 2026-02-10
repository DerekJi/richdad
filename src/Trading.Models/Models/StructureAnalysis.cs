using System.Text.Json.Serialization;

namespace Trading.Models;

/// <summary>
/// L2 - H1 结构分析结果（Structure Analysis）
/// </summary>
/// <remarks>
/// 用于存储 L2 (H1 结构层) 的分析结果，判断市场周期和交易状态。
/// AI 分析 H1 小时线后返回的市场结构、状态和与 D1 的对齐情况。
/// </remarks>
public class StructureAnalysis
{
    /// <summary>
    /// 市场周期类型
    /// </summary>
    /// <remarks>
    /// - Trend: 趋势市场（明显的上升或下降趋势）
    /// - Channel: 通道市场（在趋势通道内波动）
    /// - Range: 区间市场（横盘震荡，无明确方向）
    /// </remarks>
    [JsonPropertyName("MarketCycle")]
    public string MarketCycle { get; set; } = "";

    /// <summary>
    /// 交易状态
    /// </summary>
    /// <remarks>
    /// - Active: 活跃状态，可以寻找交易机会
    /// - Idle: 空闲状态，暂时不适合交易（等待更清晰的信号）
    ///
    /// 规则：
    /// - 如果 H1 与 D1 方向一致且处于趋势中 -> Active
    /// - 如果 H1 横盘震荡或方向不明 -> Idle
    /// </remarks>
    [JsonPropertyName("Status")]
    public string Status { get; set; } = "Idle";

    /// <summary>
    /// 是否与 D1 日线偏见对齐
    /// </summary>
    /// <remarks>
    /// true: H1 方向与 D1 偏见一致（可以交易）
    /// false: H1 方向与 D1 偏见不一致（等待或避免交易）
    /// </remarks>
    [JsonPropertyName("AlignedWithD1")]
    public bool AlignedWithD1 { get; set; }

    /// <summary>
    /// 当前市场阶段
    /// </summary>
    /// <remarks>
    /// - Breakout: 突破阶段（价格突破关键价位）
    /// - Pullback: 回调阶段（趋势中的短期回撤）
    /// - Trading Range: 交易区间阶段（横盘震荡）
    ///
    /// Al Brooks 理论中，Pullback 是最佳入场时机。
    /// </remarks>
    [JsonPropertyName("CurrentPhase")]
    public string CurrentPhase { get; set; } = "";

    /// <summary>
    /// AI 分析推理过程
    /// </summary>
    /// <remarks>
    /// AI 给出该判断的理由。
    /// 例如："H1 shows clear uptrend aligned with D1 bullish bias. Currently in pullback phase."
    /// </remarks>
    [JsonPropertyName("Reasoning")]
    public string Reasoning { get; set; } = "";

    /// <summary>
    /// 分析时间
    /// </summary>
    public DateTime AnalyzedAt { get; set; }

    /// <summary>
    /// 判断是否可以进行交易监控
    /// </summary>
    /// <remarks>
    /// 只有当 Status = Active 且与 D1 对齐时，才继续 L3 监控。
    /// </remarks>
    [JsonIgnore]
    public bool CanTrade => Status == "Active" && AlignedWithD1;

    /// <summary>
    /// 判断是否处于趋势市场
    /// </summary>
    [JsonIgnore]
    public bool IsTrending => MarketCycle == "Trend";

    /// <summary>
    /// 判断是否处于回调阶段（最佳入场时机）
    /// </summary>
    [JsonIgnore]
    public bool IsPullback => CurrentPhase == "Pullback";

    /// <summary>
    /// 判断是否处于突破阶段
    /// </summary>
    [JsonIgnore]
    public bool IsBreakout => CurrentPhase == "Breakout";

    /// <summary>
    /// 判断是否处于横盘震荡
    /// </summary>
    [JsonIgnore]
    public bool IsRangebound => MarketCycle == "Range" || CurrentPhase == "Trading Range";
}
