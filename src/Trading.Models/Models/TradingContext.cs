namespace Trading.Models;

/// <summary>
/// 完整的交易决策上下文
/// </summary>
/// <remarks>
/// 汇总四级 AI 决策编排系统的所有分析结果，提供完整的交易上下文。
///
/// 决策流程：
/// 1. L1 (D1) → 确定日内交易方向偏见
/// 2. L2 (H1) → 判断市场周期和状态（Active/Idle）
/// 3. L3 (M5) → 监控并识别交易设置
/// 4. L4 (决策) → 基于完整上下文做出最终决策
///
/// 每一级的结果都会传递给下一级，形成级联决策链。
/// 任何一级判断不满足条件，都会提前终止，避免不必要的 AI 调用成本。
/// </remarks>
public class TradingContext
{
    /// <summary>
    /// L1 - D1 日线分析结果
    /// </summary>
    /// <remarks>
    /// 包含日线级别的趋势方向、关键价位、置信度等信息。
    /// 如果 Direction = Neutral 或 Confidence < 60，流程终止。
    /// </remarks>
    public DailyBias L1_DailyBias { get; set; } = new();

    /// <summary>
    /// L2 - H1 结构分析结果
    /// </summary>
    /// <remarks>
    /// 包含小时级别的市场周期、状态、对齐性等信息。
    /// 如果 Status = Idle，流程终止。
    /// </remarks>
    public StructureAnalysis L2_Structure { get; set; } = new();

    /// <summary>
    /// L3 - M5 信号检测结果
    /// </summary>
    /// <remarks>
    /// 包含五分钟级别的交易信号、入场价、止损止盈等信息。
    /// 如果 Status = No_Signal，流程终止。
    /// </remarks>
    public SignalDetection L3_Signal { get; set; } = new();

    /// <summary>
    /// L4 - 最终决策结果（含思维链推理）
    /// </summary>
    /// <remarks>
    /// 基于 L1/L2/L3 完整上下文做出的最终交易决策。
    /// 包含 DeepSeek-R1 的 Chain of Thought 思维过程。
    /// 仅在 L3 检测到信号时才会执行 L4。
    /// </remarks>
    public FinalDecision? L4_Decision { get; set; }

    /// <summary>
    /// 市场数据（用于 AI Prompt）
    /// </summary>
    /// <remarks>
    /// 包含 Markdown 表格、形态摘要等 AI 分析所需的格式化数据。
    /// 通常是 M5 的最近数据（Focus Table）。
    /// </remarks>
    public ProcessedMarketData MarketData { get; set; } = new();

    /// <summary>
    /// 上下文创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 品种代码
    /// </summary>
    public string Symbol { get; set; } = "";

    /// <summary>
    /// 判断 L1 是否通过验证（可以继续 L2）
    /// </summary>
    public bool IsL1Valid =>
        L1_DailyBias.Direction != "Neutral" &&
        L1_DailyBias.Confidence >= 60;

    /// <summary>
    /// 判断 L2 是否通过验证（可以继续 L3）
    /// </summary>
    public bool IsL2Valid =>
        L2_Structure.Status == "Active" &&
        L2_Structure.AlignedWithD1;

    /// <summary>
    /// 判断 L3 是否通过验证（可以继续 L4）
    /// </summary>
    public bool IsL3Valid =>
        L3_Signal.Status == "Potential_Setup" &&
        L3_Signal.HasSignal;

    /// <summary>
    /// 判断所有层级是否对齐（可以执行 L4 决策）
    /// </summary>
    public bool IsFullyAligned => IsL1Valid && IsL2Valid && IsL3Valid;

    /// <summary>
    /// 获取被终止的层级（用于日志记录）
    /// </summary>
    /// <returns>
    /// - "L1": D1 分析未通过
    /// - "L2": H1 结构分析未通过
    /// - "L3": M5 信号检测未通过
    /// - "None": 所有层级都通过
    /// </returns>
    public string GetTerminatedLevel()
    {
        if (!IsL1Valid) return "L1";
        if (!IsL2Valid) return "L2";
        if (!IsL3Valid) return "L3";
        return "None";
    }

    /// <summary>
    /// 获取终止原因
    /// </summary>
    public string GetTerminationReason()
    {
        if (!IsL1Valid)
        {
            if (L1_DailyBias.Direction == "Neutral")
                return "D1 bias is Neutral";
            if (L1_DailyBias.Confidence < 60)
                return $"D1 confidence too low ({L1_DailyBias.Confidence}%)";
        }

        if (!IsL2Valid)
        {
            if (L2_Structure.Status != "Active")
                return "H1 status is Idle";
            if (!L2_Structure.AlignedWithD1)
                return "H1 not aligned with D1";
        }

        if (!IsL3Valid)
        {
            return "No trading setup detected on M5";
        }

        return "All levels passed";
    }

    /// <summary>
    /// 生成上下文摘要（用于日志或通知）
    /// </summary>
    public string GetSummary()
    {
        return $@"Trading Context for {Symbol} @ {CreatedAt:yyyy-MM-dd HH:mm}

L1 (D1): {L1_DailyBias.Direction} ({L1_DailyBias.Confidence}% confidence)
         Trend: {L1_DailyBias.TrendType}

L2 (H1): {L2_Structure.MarketCycle}, Status: {L2_Structure.Status}
         Phase: {L2_Structure.CurrentPhase}, Aligned: {L2_Structure.AlignedWithD1}

L3 (M5): {L3_Signal.Status}
         Setup: {L3_Signal.SetupType} ({L3_Signal.Direction})

Validation: L1={IsL1Valid}, L2={IsL2Valid}, L3={IsL3Valid}
Can Trade: {IsFullyAligned}";
    }
}
