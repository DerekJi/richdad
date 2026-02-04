namespace Trading.Data.Configuration;

/// <summary>
/// 账户配置（回测和实盘通用）
/// </summary>
public class AccountSettings
{
    /// <summary>
    /// 杠杆率
    /// </summary>
    public double Leverage { get; set; }

    /// <summary>
    /// 初始资金 (USD)
    /// </summary>
    public double InitialCapital { get; set; }

    /// <summary>
    /// 单笔最大亏损百分比
    /// </summary>
    public double MaxLossPerTradePercent { get; set; }

    /// <summary>
    /// 单日最大亏损百分比
    /// </summary>
    public double MaxDailyLossPercent { get; set; }

    /// <summary>
    /// 启用动态风险管理（连续亏损后逐级减半风险）
    /// </summary>
    public bool EnableDynamicRiskManagement { get; set; }

    /// <summary>
    /// 动态风险管理触发阈值（连续亏损多少次后减半风险，默认3次）
    /// 仅在EnableDynamicRiskManagement=true时生效
    /// </summary>
    public int DynamicRiskLossThreshold { get; set; } = 3;
}
