namespace Trading.Data.Models;

/// <summary>
/// 止损策略类型
/// </summary>
public enum StopLossStrategy
{
    PinbarEndPlusAtr    // Pin Bar端点加ATR倍数
}

/// <summary>
/// ADX计算使用的时间周期
/// </summary>
public enum AdxTimeframe
{
    Current,    // 当前周期（M15）
    H1,         // 1小时
    H4,         // 4小时
    Daily       // 日线
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
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// CSV文件名应包含的过滤字符串
    /// </summary>
    public string CsvFilter { get; set; } = string.Empty;

    /// <summary>
    /// 合约规模
    /// </summary>
    public decimal ContractSize { get; set; }

    /// <summary>
    /// 基准EMA周期
    /// </summary>
    public int BaseEma { get; set; }

    /// <summary>
    /// K线最小阈值(美元)，过滤波动太小的K线
    /// </summary>
    public decimal Threshold { get; set; }

    /// <summary>
    /// 实体最大占比(%)
    /// </summary>
    public decimal MaxBodyPercentage { get; set; }

    /// <summary>
    /// 长影线最小占比(%)
    /// </summary>
    public decimal MinLongerWickPercentage { get; set; }

    /// <summary>
    /// 短影线最大占比(%)
    /// </summary>
    public decimal MaxShorterWickPercentage { get; set; }

    /// <summary>
    /// EMA列表，用于判断是否靠近
    /// </summary>
    public List<int> EmaList { get; set; } = new();

    /// <summary>
    /// 靠近EMA的阈值(美元)
    /// </summary>
    public decimal NearEmaThreshold { get; set; }

    /// <summary>
    /// 盈亏比
    /// </summary>
    public decimal RiskRewardRatio { get; set; }

    /// <summary>
    /// 止损ATR倍数
    /// </summary>
    public decimal StopLossAtrRatio { get; set; }

    /// <summary>
    /// 止损策略
    /// </summary>
    public StopLossStrategy StopLossStrategy { get; set; } = StopLossStrategy.PinbarEndPlusAtr;

    /// <summary>
    /// 开始交易时间 (UTC小时)
    /// </summary>
    public int StartTradingHour { get; set; }

    /// <summary>
    /// 结束交易时间 (UTC小时)
    /// </summary>
    public int EndTradingHour { get; set; }

    /// <summary>
    /// 不限制交易时段（true表示24小时交易）
    /// </summary>
    public bool NoTradingHoursLimit { get; set; } = false;

    /// <summary>
    /// 禁止开单的小时（UTC时间）
    /// </summary>
    public List<int> NoTradeHours { get; set; } = new();

    /// <summary>
    /// ATR周期
    /// </summary>
    public int AtrPeriod { get; set; }

    /// <summary>
    /// 是否要求Pin Bar为阳线/阴线
    /// </summary>
    public bool RequirePinBarDirectionMatch { get; set; } = false;

    /// <summary>
    /// Pin Bar下影线最小ATR倍数
    /// </summary>
    public decimal MinLowerWickAtrRatio { get; set; }

    /// <summary>
    /// 最小ADX值（趋势强度过滤），0表示不使用ADX过滤
    /// ADX > 25: 强趋势
    /// ADX < 20: 震荡市
    /// </summary>
    public decimal MinAdx { get; set; } = 0;

    /// <summary>
    /// ADX周期
    /// </summary>
    public int AdxPeriod { get; set; } = 14;

    /// <summary>
    /// <summary>
    /// 低ADX时使用的盈亏比（震荡市场），0表示使用标准盈亏比
    /// 当ADX < MinAdx时，使用此较小的盈亏比快速获利
    /// </summary>
    public decimal LowAdxRiskRewardRatio { get; set; } = 0;

    /// <summary>
    /// ADX计算使用的时间周期，默认使用当前周期
    /// 使用更高周期的ADX可以更好地反映大趋势强度
    /// </summary>
    public AdxTimeframe AdxTimeframe { get; set; } = AdxTimeframe.Current;

    /// <summary>
    /// 连续亏损多少次后暂停交易，0表示不启用此规则
    /// </summary>
    public int MaxConsecutiveLosses { get; set; } = 0;

    /// <summary>
    /// 连续亏损后暂停交易的天数
    /// </summary>
    public int PauseDaysAfterLosses { get; set; } = 5;

    /// <summary>
    /// 创建默认XAGUSD配置
    /// </summary>
    public static StrategyConfig CreateXagDefault() => new()
    {
        Symbol = "XAGUSD",
        ContractSize = 1000,
        NearEmaThreshold = 0.2m
    };
}
