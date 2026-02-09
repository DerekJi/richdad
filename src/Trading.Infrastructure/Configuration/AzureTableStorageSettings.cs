namespace Trading.Infrastructure.Configuration;

/// <summary>
/// Azure Table Storage 配置
/// </summary>
public class AzureTableStorageSettings
{
    public const string SectionName = "AzureTableStorage";

    /// <summary>
    /// 连接字符串
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用 Azure Table Storage
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 价格监控表名
    /// </summary>
    public string PriceMonitorTableName { get; set; } = "PriceMonitor";

    /// <summary>
    /// 告警历史表名
    /// </summary>
    public string AlertHistoryTableName { get; set; } = "AlertHistory";

    /// <summary>
    /// EMA监控表名
    /// </summary>
    public string EmaMonitorTableName { get; set; } = "EmaMonitor";

    /// <summary>
    /// 数据源配置表名
    /// </summary>
    public string DataSourceConfigTableName { get; set; } = "DataSourceConfig";

    /// <summary>
    /// 邮件配置表名
    /// </summary>
    public string EmailConfigTableName { get; set; } = "EmailConfig";

    /// <summary>
    /// PinBar监控表名
    /// </summary>
    public string PinBarMonitorTableName { get; set; } = "PinBarMonitor";

    /// <summary>
    /// PinBar信号表名
    /// </summary>
    public string PinBarSignalTableName { get; set; } = "PinBarSignal";

    /// <summary>
    /// AI分析历史表名
    /// </summary>
    public string AIAnalysisHistoryTableName { get; set; } = "AIAnalysisHistory";
}
