using Trading.AlertSystem.Service.Models;

namespace Trading.AlertSystem.Service.Services;

/// <summary>
/// EMA监测服务接口
/// </summary>
public interface IEmaMonitoringService
{
    /// <summary>
    /// 开始监测
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// 停止监测
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// 执行一次检查
    /// </summary>
    Task CheckAsync();

    /// <summary>
    /// 获取当前监测状态
    /// </summary>
    Task<IEnumerable<EmaMonitoringState>> GetStatesAsync();
}
