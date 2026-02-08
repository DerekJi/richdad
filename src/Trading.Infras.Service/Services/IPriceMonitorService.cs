using Trading.AlertSystem.Data.Models;

namespace Trading.AlertSystem.Service.Services;

/// <summary>
/// 价格监控服务接口
/// </summary>
public interface IPriceMonitorService
{
    /// <summary>
    /// 开始监控
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// 停止监控
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// 执行一次监控检查
    /// </summary>
    Task ExecuteCheckAsync();

    /// <summary>
    /// 检查单个监控规则
    /// </summary>
    Task<bool> CheckRuleAsync(PriceMonitorRule rule);
}
