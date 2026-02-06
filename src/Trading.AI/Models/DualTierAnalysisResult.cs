namespace Trading.AI.Models;

/// <summary>
/// 双级分析结果
/// </summary>
public class DualTierAnalysisResult
{
    /// <summary>
    /// Tier1 过滤结果
    /// </summary>
    public Tier1FilterResult? Tier1Result { get; set; }

    /// <summary>
    /// Tier2 深度分析结果（仅当Tier1通过后才有值）
    /// </summary>
    public Tier2AnalysisResult? Tier2Result { get; set; }

    /// <summary>
    /// 是否通过Tier1过滤
    /// </summary>
    public bool PassedTier1 => Tier1Result?.OpportunityScore >= 70;

    /// <summary>
    /// 是否建议入场
    /// </summary>
    public bool ShouldEnter => PassedTier1 && Tier2Result?.Action == "BUY";

    /// <summary>
    /// 总处理时间（毫秒）
    /// </summary>
    public long TotalProcessingTimeMs { get; set; }

    /// <summary>
    /// 总成本（美元）
    /// </summary>
    public decimal TotalCostUsd { get; set; }
}

/// <summary>
/// Tier1 过滤结果（GPT-4o-mini）
/// </summary>
public class Tier1FilterResult
{
    /// <summary>
    /// 机会评分 (0-100)
    /// </summary>
    public int OpportunityScore { get; set; }

    /// <summary>
    /// 趋势方向 (Bullish, Bearish, Neutral)
    /// </summary>
    public string TrendDirection { get; set; } = "Neutral";

    /// <summary>
    /// 简要理由
    /// </summary>
    public string Reasoning { get; set; } = string.Empty;

    /// <summary>
    /// 处理时间（毫秒）
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// 使用的Token数量
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    /// 成本（美元）
    /// </summary>
    public decimal CostUsd { get; set; }

    /// <summary>
    /// 拦截原因（当Score < 70时）
    /// </summary>
    public string? RejectionReason { get; set; }
}

/// <summary>
/// Tier2 深度分析结果（GPT-4o）
/// </summary>
public class Tier2AnalysisResult
{
    /// <summary>
    /// 交易动作 (BUY, SELL, HOLD)
    /// </summary>
    public string Action { get; set; } = "HOLD";

    /// <summary>
    /// 入场价格
    /// </summary>
    public decimal? EntryPrice { get; set; }

    /// <summary>
    /// 止损价格
    /// </summary>
    public decimal? StopLoss { get; set; }

    /// <summary>
    /// 止盈价格
    /// </summary>
    public decimal? TakeProfit { get; set; }

    /// <summary>
    /// 建议手数
    /// </summary>
    public decimal? LotSize { get; set; }

    /// <summary>
    /// 风险金额（美元）
    /// </summary>
    public decimal? RiskAmountUsd { get; set; }

    /// <summary>
    /// 风险回报比
    /// </summary>
    public decimal? RiskRewardRatio { get; set; }

    /// <summary>
    /// 支撑位分析
    /// </summary>
    public string? SupportAnalysis { get; set; }

    /// <summary>
    /// 阻力位分析
    /// </summary>
    public string? ResistanceAnalysis { get; set; }

    /// <summary>
    /// 假突破风险评估
    /// </summary>
    public string? StopRunRisk { get; set; }

    /// <summary>
    /// 多周期共振分析
    /// </summary>
    public string? MultiTimeframeAnalysis { get; set; }

    /// <summary>
    /// 完整推理过程
    /// </summary>
    public string Reasoning { get; set; } = string.Empty;

    /// <summary>
    /// 处理时间（毫秒）
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// 使用的Token数量
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    /// 成本（美元）
    /// </summary>
    public decimal CostUsd { get; set; }

    /// <summary>
    /// Tier1的总结（作为前置参考）
    /// </summary>
    public string? Tier1Summary { get; set; }
}
