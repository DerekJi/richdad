namespace Trading.Data.Configuration;

/// <summary>
/// 技术指标通用配置
/// </summary>
public class IndicatorSettings
{
    /// <summary>
    /// 基准EMA周期
    /// </summary>
    public int BaseEma { get; set; }
    
    /// <summary>
    /// ATR周期
    /// </summary>
    public int AtrPeriod { get; set; }
    
    /// <summary>
    /// 快速EMA周期
    /// </summary>
    public int EmaFastPeriod { get; set; } = 20;
    
    /// <summary>
    /// 慢速EMA周期
    /// </summary>
    public int EmaSlowPeriod { get; set; } = 60;
}
