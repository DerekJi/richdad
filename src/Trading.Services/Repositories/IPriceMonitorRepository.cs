using Trading.Infrastructure.Models;

namespace Trading.Services.Repositories;

/// <summary>
/// 价格监控仓储接口
/// </summary>
public interface IPriceMonitorRepository
{
    /// <summary>
    /// 获取所有监控规则
    /// </summary>
    Task<IEnumerable<PriceMonitorRule>> GetAllAsync();

    /// <summary>
    /// 获取启用的监控规则
    /// </summary>
    Task<IEnumerable<PriceMonitorRule>> GetEnabledRulesAsync();

    /// <summary>
    /// 获取已触发的监控规则
    /// </summary>
    Task<IEnumerable<PriceMonitorRule>> GetTriggeredRulesAsync();

    /// <summary>
    /// 根据ID获取监控规则
    /// </summary>
    Task<PriceMonitorRule?> GetByIdAsync(string id);

    /// <summary>
    /// 创建监控规则
    /// </summary>
    Task<PriceMonitorRule> CreateAsync(PriceMonitorRule rule);

    /// <summary>
    /// 更新监控规则
    /// </summary>
    Task<PriceMonitorRule?> UpdateAsync(PriceMonitorRule rule);

    /// <summary>
    /// 删除监控规则
    /// </summary>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// 标记监控规则已触发
    /// </summary>
    Task MarkAsTriggeredAsync(string id);

    /// <summary>
    /// 重置监控规则状态
    /// </summary>
    Task ResetRuleAsync(string id);
}
