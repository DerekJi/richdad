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
}
