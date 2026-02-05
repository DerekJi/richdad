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
    /// 价格提醒容器名称
    /// </summary>
    public string AlertContainerName { get; set; } = "PriceAlerts";

    /// <summary>
    /// 告警历史容器名称
    /// </summary>
    public string AlertHistoryContainerName { get; set; } = "AlertHistory";

    /// <summary>
    /// EMA配置容器名称
    /// </summary>
    public string EmaConfigContainerName { get; set; } = "EmaConfig";

    /// <summary>
    /// 数据源配置容器名称
    /// </summary>
    public string DataSourceConfigContainerName { get; set; } = "DataSourceConfig";

    /// <summary>
    /// 邮件配置容器名称
    /// </summary>
    public string EmailConfigContainerName { get; set; } = "EmailConfig";
}
