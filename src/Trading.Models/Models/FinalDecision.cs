using System.Text.Json.Serialization;

namespace Trading.Models;

/// <summary>
/// L4 - 最终交易决策（Final Decision）
/// </summary>
/// <remarks>
/// 用于存储 L4 (决策层) 的最终决策结果。
/// 由 DeepSeek-R1 推理模型基于 L1/L2/L3 的完整上下文，
/// 经过批判性思维（Chain of Thought）后给出的最终执行或拒绝决定。
/// </remarks>
public class FinalDecision
{
    /// <summary>
    /// 决策动作
    /// </summary>
    /// <remarks>
    /// - Execute: 执行交易（所有条件满足，高概率设置）
    /// - Reject: 拒绝交易（存在疑虑或条件不满足）
    ///
    /// Al Brooks 理论强调：如果有任何疑虑，不要交易。
    /// 只交易最佳设置，保证 60%+ 胜率。
    /// </remarks>
    [JsonPropertyName("Action")]
    public string Action { get; set; } = "Reject";

    /// <summary>
    /// 交易方向
    /// </summary>
    /// <remarks>
    /// - Buy: 做多
    /// - Sell: 做空
    /// </remarks>
    [JsonPropertyName("Direction")]
    public string Direction { get; set; } = "";

    /// <summary>
    /// 最终确定的入场价格
    /// </summary>
    /// <remarks>
    /// L4 可能会基于市场状态微调 L3 建议的入场价。
    /// </remarks>
    [JsonPropertyName("EntryPrice")]
    public double EntryPrice { get; set; }

    /// <summary>
    /// 最终确定的止损价格
    /// </summary>
    [JsonPropertyName("StopLoss")]
    public double StopLoss { get; set; }

    /// <summary>
    /// 最终确定的止盈价格
    /// </summary>
    [JsonPropertyName("TakeProfit")]
    public double TakeProfit { get; set; }

    /// <summary>
    /// 手数大小（Lot Size）
    /// </summary>
    /// <remarks>
    /// 基于风险管理计算的仓位大小。
    /// 通常根据账户余额的固定百分比（如 1-2%）和止损距离计算。
    /// </remarks>
    [JsonPropertyName("LotSize")]
    public double LotSize { get; set; }

    /// <summary>
    /// 最终决策推理
    /// </summary>
    /// <remarks>
    /// AI 给出最终决定的理由（简洁版）。
    /// 例如："Execute: Strong alignment across all timeframes. H2 setup with good RR ratio."
    /// 或 "Reject: Stop loss too wide, RR ratio only 0.8:1"
    /// </remarks>
    [JsonPropertyName("Reasoning")]
    public string Reasoning { get; set; } = "";

    /// <summary>
    /// 思维链推理过程（Chain of Thought）
    /// </summary>
    /// <remarks>
    /// DeepSeek-R1 模型的完整推理过程，展示从分析到决策的每一步思考。
    /// 这是 R1 模型的核心优势，提供可解释的决策路径。
    /// 通常包含：
    /// 1. 上下文总结
    /// 2. 对齐性检查
    /// 3. 风险评估
    /// 4. 潜在问题识别
    /// 5. 最终结论
    /// </remarks>
    [JsonPropertyName("ThinkingProcess")]
    public string ThinkingProcess { get; set; } = "";

    /// <summary>
    /// 置信度评分 (0-100)
    /// </summary>
    /// <remarks>
    /// AI 对该决策的信心程度。
    /// 通常要求 >= 75 才会执行交易（Execute）。
    /// </remarks>
    [JsonPropertyName("ConfidenceScore")]
    public int ConfidenceScore { get; set; }

    /// <summary>
    /// 风险因素列表
    /// </summary>
    /// <remarks>
    /// AI 识别出的潜在风险点。
    /// 例如：["Wide stop loss", "Unclear momentum", "Near key resistance"]
    ///
    /// 即使决定 Execute，也应该列出风险因素，便于后续复盘。
    /// </remarks>
    [JsonPropertyName("RiskFactors")]
    public List<string> RiskFactors { get; set; } = new();

    /// <summary>
    /// 决策时间
    /// </summary>
    public DateTime DecidedAt { get; set; }

    /// <summary>
    /// 判断是否执行交易
    /// </summary>
    [JsonIgnore]
    public bool ShouldExecute => Action == "Execute";

    /// <summary>
    /// 判断是否拒绝交易
    /// </summary>
    [JsonIgnore]
    public bool IsRejected => Action == "Reject";

    /// <summary>
    /// 判断置信度是否足够高
    /// </summary>
    /// <param name="minConfidence">最小置信度要求（默认 75）</param>
    public bool IsHighConfidence(int minConfidence = 75) => ConfidenceScore >= minConfidence;

    /// <summary>
    /// 计算风险金额
    /// </summary>
    [JsonIgnore]
    public double RiskAmount => Math.Abs(EntryPrice - StopLoss);

    /// <summary>
    /// 计算回报金额
    /// </summary>
    [JsonIgnore]
    public double RewardAmount => Math.Abs(TakeProfit - EntryPrice);

    /// <summary>
    /// 计算风险回报比
    /// </summary>
    [JsonIgnore]
    public double RiskRewardRatio
    {
        get
        {
            if (RiskAmount == 0) return 0;
            return RewardAmount / RiskAmount;
        }
    }

    /// <summary>
    /// 计算潜在风险金额（基于手数）
    /// </summary>
    /// <remarks>
    /// 实际账户风险 = 风险金额 × 手数
    /// </remarks>
    [JsonIgnore]
    public double TotalRiskAmount => RiskAmount * LotSize;

    /// <summary>
    /// 计算潜在盈利金额（基于手数）
    /// </summary>
    [JsonIgnore]
    public double TotalRewardAmount => RewardAmount * LotSize;

    /// <summary>
    /// 获取风险因素数量
    /// </summary>
    [JsonIgnore]
    public int RiskFactorCount => RiskFactors.Count;

    /// <summary>
    /// 判断是否存在高风险因素（>= 3 个）
    /// </summary>
    [JsonIgnore]
    public bool HasHighRisk => RiskFactorCount >= 3;
}
