using System.Text.Json.Serialization;

namespace Trading.Models;

/// <summary>
/// L3 - M5 信号检测结果（Signal Detection）
/// </summary>
/// <remarks>
/// 用于存储 L3 (M5 监控层) 的信号检测结果，识别 Al Brooks 交易设置。
/// AI 分析 M5 五分钟线后返回的潜在交易机会。
/// </remarks>
public class SignalDetection
{
    /// <summary>
    /// 信号状态
    /// </summary>
    /// <remarks>
    /// - Potential_Setup: 检测到潜在交易设置，需要 L4 最终决策
    /// - No_Signal: 未检测到交易信号
    /// </remarks>
    [JsonPropertyName("Status")]
    public string Status { get; set; } = "No_Signal";

    /// <summary>
    /// 交易设置类型（Al Brooks 形态）
    /// </summary>
    /// <remarks>
    /// - H2/L2: 第二次入场（Second Entry），趋势中的最佳入场点
    /// - MTR: 主要趋势反转（Major Trend Reversal），在关键价位的反转
    /// - Gap_Bar: 跳空棒，EMA20 缺口配合强势动能
    /// - ii_Breakout: 内包线结构突破，波动收缩后的爆发
    /// - 其他 Al Brooks 经典设置
    /// </remarks>
    [JsonPropertyName("SetupType")]
    public string SetupType { get; set; } = "";

    /// <summary>
    /// 建议入场价格
    /// </summary>
    /// <remarks>
    /// 基于信号棒（Signal Bar）的位置建议的入场价。
    /// 通常是信号棒收盘价或下一根棒的开盘价附近。
    /// </remarks>
    [JsonPropertyName("EntryPrice")]
    public double EntryPrice { get; set; }

    /// <summary>
    /// 建议止损价格
    /// </summary>
    /// <remarks>
    /// 基于信号棒的低点/高点设置止损。
    /// 多头：信号棒低点下方若干 ticks
    /// 空头：信号棒高点上方若干 ticks
    /// </remarks>
    [JsonPropertyName("StopLoss")]
    public double StopLoss { get; set; }

    /// <summary>
    /// 建议止盈价格
    /// </summary>
    /// <remarks>
    /// 基于风险回报比（通常 1:2 或更高）和关键价位设置。
    /// Al Brooks 建议至少保证 1:1 的风险回报比。
    /// </remarks>
    [JsonPropertyName("TakeProfit")]
    public double TakeProfit { get; set; }

    /// <summary>
    /// 交易方向
    /// </summary>
    /// <remarks>
    /// - Buy: 做多（看涨设置）
    /// - Sell: 做空（看跌设置）
    /// </remarks>
    [JsonPropertyName("Direction")]
    public string Direction { get; set; } = "";

    /// <summary>
    /// AI 分析推理过程
    /// </summary>
    /// <remarks>
    /// AI 给出识别该信号的理由。
    /// 例如："H2 setup detected. Second bull bar after pullback to EMA20 in strong uptrend."
    /// </remarks>
    [JsonPropertyName("Reasoning")]
    public string Reasoning { get; set; } = "";

    /// <summary>
    /// 信号检测时间
    /// </summary>
    public DateTime DetectedAt { get; set; }

    /// <summary>
    /// 判断是否检测到有效信号
    /// </summary>
    [JsonIgnore]
    public bool HasSignal => Status == "Potential_Setup";

    /// <summary>
    /// 判断是否为做多信号
    /// </summary>
    [JsonIgnore]
    public bool IsBuySignal => Direction == "Buy";

    /// <summary>
    /// 判断是否为做空信号
    /// </summary>
    [JsonIgnore]
    public bool IsSellSignal => Direction == "Sell";

    /// <summary>
    /// 计算风险金额（止损距离）
    /// </summary>
    [JsonIgnore]
    public double RiskAmount => Math.Abs(EntryPrice - StopLoss);

    /// <summary>
    /// 计算回报金额（止盈距离）
    /// </summary>
    [JsonIgnore]
    public double RewardAmount => Math.Abs(TakeProfit - EntryPrice);

    /// <summary>
    /// 计算风险回报比（Reward/Risk）
    /// </summary>
    /// <remarks>
    /// Al Brooks 建议至少 1:1，理想情况 1:2 或更高。
    /// </remarks>
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
    /// 判断风险回报比是否合理（>= 1:1）
    /// </summary>
    [JsonIgnore]
    public bool IsGoodRiskReward => RiskRewardRatio >= 1.0;

    /// <summary>
    /// 判断是否为 H2/L2 设置（最佳入场点）
    /// </summary>
    [JsonIgnore]
    public bool IsSecondEntry => SetupType == "H2" || SetupType == "L2";

    /// <summary>
    /// 判断是否为主要趋势反转
    /// </summary>
    [JsonIgnore]
    public bool IsMTR => SetupType == "MTR";
}
