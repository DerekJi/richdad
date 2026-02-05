namespace Trading.AlertSystem.Data.Configuration;

/// <summary>
/// 数据源配置
/// </summary>
public class DataSourceSettings
{
    /// <summary>
    /// 数据提供商：TradeLocker 或 Oanda
    /// </summary>
    public string Provider { get; set; } = "Oanda";
}
