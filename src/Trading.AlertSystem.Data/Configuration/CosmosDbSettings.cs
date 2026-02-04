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
}
