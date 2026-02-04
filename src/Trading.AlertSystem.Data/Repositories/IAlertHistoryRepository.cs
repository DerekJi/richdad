using Trading.AlertSystem.Data.Models;

namespace Trading.AlertSystem.Data.Repositories;

/// <summary>
/// 告警历史仓储接口
/// </summary>
public interface IAlertHistoryRepository
{
    /// <summary>
    /// 添加告警历史记录
    /// </summary>
    Task<AlertHistory> AddAsync(AlertHistory alertHistory);

    /// <summary>
    /// 根据ID获取告警历史记录
    /// </summary>
    Task<AlertHistory?> GetByIdAsync(string id);

    /// <summary>
    /// 获取所有告警历史记录（支持分页）
    /// </summary>
    Task<(IEnumerable<AlertHistory> Items, int TotalCount)> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 50,
        AlertHistoryType? type = null,
        string? symbol = null,
        DateTime? startTime = null,
        DateTime? endTime = null);

    /// <summary>
    /// 获取最近的告警历史记录
    /// </summary>
    Task<IEnumerable<AlertHistory>> GetRecentAsync(int count = 100);

    /// <summary>
    /// 根据品种获取告警历史
    /// </summary>
    Task<IEnumerable<AlertHistory>> GetBySymbolAsync(string symbol, int limit = 100);

    /// <summary>
    /// 根据类型获取告警历史
    /// </summary>
    Task<IEnumerable<AlertHistory>> GetByTypeAsync(AlertHistoryType type, int limit = 100);

    /// <summary>
    /// 删除指定时间之前的告警历史
    /// </summary>
    Task<int> DeleteOldRecordsAsync(DateTime beforeDate);
}
