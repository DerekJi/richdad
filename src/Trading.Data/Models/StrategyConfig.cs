namespace Trading.Data.Models;

/// <summary>
/// 止损策略类型
/// </summary>
public enum StopLossStrategy
{
    PinbarEndPlusAtr    // Pin Bar端点加ATR倍数
}

/// <summary>
/// 交易策略配置
/// </summary>
public class StrategyConfig
{
    /// <summary>
    /// 策略名称
    /// </summary>
    public string StrategyName { get; set; } = "PinBar";
    
    /// <summary>
    /// 交易品种 (如 XAUUSD, XAGUSD)
    /// </summary>
    public string Symbol { get; set; } = "XAUUSD";
    
    /// <summary>
    /// 合约规模
    /// </summary>
    public decimal ContractSize { get; set; } = 100;
    
    /// <summary>
    /// 基准EMA周期
    /// </summary>
    public int BaseEma { get; set; } = 200;
    
    /// <summary>
    /// K线最小阈值(美元)，过滤波动太小的K线
    /// </summary>
    public decimal Threshold { get; set; } = 1.0m;
    
    /// <summary>
    /// 实体最大占比(%)
    /// </summary>
    public decimal MaxBodyPercentage { get; set; } = 30;
    
    /// <summary>
    /// 长影线最小占比(%)
    /// </summary>
    public decimal MinLongerWickPercentage { get; set; } = 60;
    
    /// <summary>
    /// 短影线最大占比(%)
    /// </summary>
    public decimal MaxShorterWickPercentage { get; set; } = 20;
    
    /// <summary>
    /// EMA列表，用于判断是否靠近
    /// </summary>
    public List<int> EmaList { get; set; } = new() { 20, 60, 80, 100, 200 };
    
    /// <summary>
    /// 靠近EMA的阈值(美元)
    /// </summary>
    public decimal NearEmaThreshold { get; set; } = 0.8m;
    
    /// <summary>
    /// 盈亏比
    /// </summary>
    public decimal RiskRewardRatio { get; set; } = 1.5m;
    
    /// <summary>
    /// 止损ATR倍数
    /// </summary>
    public decimal StopLossAtrRatio { get; set; } = 1.0m;
    
    /// <summary>
    /// 止损策略
    /// </summary>
    public StopLossStrategy StopLossStrategy { get; set; } = StopLossStrategy.PinbarEndPlusAtr;
    
    /// <summary>
    /// 开始交易时间 (UTC小时)
    /// </summary>
    public int StartTradingHour { get; set; } = 5;
    
    /// <summary>
    /// 结束交易时间 (UTC小时)
    /// </summary>
    public int EndTradingHour { get; set; } = 11;
    
    /// <summary>
    /// ATR周期
    /// </summary>
    public int AtrPeriod { get; set; } = 14;
    
    /// <summary>
    /// 是否要求Pin Bar为阳线/阴线
    /// </summary>
    public bool RequirePinBarDirectionMatch { get; set; } = false;
    
    /// <summary>
    /// Pin Bar下影线最小ATR倍数
    /// </summary>
    public decimal MinLowerWickAtrRatio { get; set; } = 1.2m;
    
    /// <summary>
    /// 创建默认XAUUSD配置
    /// </summary>
    public static StrategyConfig CreateXauDefault() => new()
    {
        Symbol = "XAUUSD",
        ContractSize = 100,
        Threshold = 1.0m,
        NearEmaThreshold = 0.8m
    };
    
    /// <summary>
    /// 创建默认XAGUSD配置
    /// </summary>
    public static StrategyConfig CreateXagDefault() => new()
    {
        Symbol = "XAGUSD",
        ContractSize = 1000,
        Threshold = 0.8m,
        NearEmaThreshold = 0.2m
    };
}
