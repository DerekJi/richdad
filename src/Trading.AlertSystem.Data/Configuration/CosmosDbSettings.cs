namespace Trading.AlertSystem.Data.Configuration;

/// <summary>
/// Cosmos DB 配置设置
/// </summary>
public class CosmosDbSettings
{
    /// <summary>
    /// Cosmos DB 连接字符串
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// 数据库名称
    /// </summary>
    public string DatabaseName { get; set; } = "TradingSystem";

    /// <summary>
    /// 价格监控容器名称
    /// </summary>
    public string PriceMonitorContainerName { get; set; } = "PriceMonitor";

    /// <summary>
    /// 告警历史容器名称
    /// </summary>
    public string AlertHistoryContainerName { get; set; } = "AlertHistory";

    /// <summary>
    /// EMA监控配置容器名称
    /// </summary>
    public string EmaMonitorContainerName { get; set; } = "EmaMonitor";

    /// <summary>
    /// 数据源配置容器名称
    /// </summary>
    public string DataSourceConfigContainerName { get; set; } = "DataSourceConfig";

    /// <summary>
    /// 邮件配置容器名称
    /// </summary>
    public string EmailConfigContainerName { get; set; } = "EmailConfig";

    /// <summary>
    /// AI分析历史容器名称
    /// </summary>
    public string AIAnalysisHistoryContainerName { get; set; } = "AIAnalysisHistory";
}
