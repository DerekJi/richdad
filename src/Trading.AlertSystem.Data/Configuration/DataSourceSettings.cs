namespace Trading.AlertSystem.Data.Configuration;

/// <summary>
/// 数据源配置（从数据库动态加载）
/// </summary>
public class DataSourceSettings
{
    /// <summary>
    /// 数据提供商：TradeLocker 或 Oanda（默认Oanda）
    /// </summary>
    public string Provider { get; set; } = "Oanda";
}
