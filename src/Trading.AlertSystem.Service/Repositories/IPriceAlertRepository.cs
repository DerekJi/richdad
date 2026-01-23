using Trading.AlertSystem.Data.Models;

namespace Trading.AlertSystem.Service.Repositories;

/// <summary>
/// 价格告警仓储接口
/// </summary>
public interface IPriceAlertRepository
{
    /// <summary>
    /// 获取所有告警
    /// </summary>
    Task<IEnumerable<PriceAlert>> GetAllAsync();

    /// <summary>
    /// 获取启用的告警
    /// </summary>
    Task<IEnumerable<PriceAlert>> GetEnabledAlertsAsync();

    /// <summary>
    /// 根据ID获取告警
    /// </summary>
    Task<PriceAlert?> GetByIdAsync(string id);

    /// <summary>
    /// 创建告警
    /// </summary>
    Task<PriceAlert> CreateAsync(PriceAlert alert);

    /// <summary>
    /// 更新告警
    /// </summary>
    Task<PriceAlert?> UpdateAsync(PriceAlert alert);

    /// <summary>
    /// 删除告警
    /// </summary>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// 标记告警已触发
    /// </summary>
    Task MarkAsTriggeredAsync(string id);

    /// <summary>
    /// 重置告警状态
    /// </summary>
    Task ResetAlertAsync(string id);
}
