namespace Trading.Infras.Service.Configuration;

/// <summary>
/// 监控服务配置
/// </summary>
public class MonitoringSettings
{
    /// <summary>
    /// 监控间隔（秒）
    /// </summary>
    public int IntervalSeconds { get; set; } = 60;

    /// <summary>
    /// 是否启用监控
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 是否在启动时立即执行一次
    /// </summary>
    public bool RunOnStartup { get; set; } = true;

    /// <summary>
    /// 最大并发监控数量
    /// </summary>
    public int MaxConcurrency { get; set; } = 10;
}
