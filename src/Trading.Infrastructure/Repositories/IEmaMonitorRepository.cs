using Trading.Infrastructure.Models;

namespace Trading.Infrastructure.Repositories;

/// <summary>
/// EMA监控配置仓储接口
/// </summary>
public interface IEmaMonitorRepository
{
    /// <summary>
    /// 获取配置（单例模式）
    /// </summary>
    Task<EmaMonitoringConfig?> GetConfigAsync();

    /// <summary>
    /// 保存或更新配置
    /// </summary>
    Task<EmaMonitoringConfig> SaveConfigAsync(EmaMonitoringConfig config);

    /// <summary>
    /// 使用默认配置初始化（如果不存在）
    /// </summary>
    Task<EmaMonitoringConfig> InitializeDefaultConfigAsync(
        bool enabled,
        List<string> symbols,
        List<string> timeFrames,
        List<int> emaPeriods,
        int historyMultiplier);

    /// <summary>
    /// 删除配置
    /// </summary>
    Task DeleteConfigAsync();
}
