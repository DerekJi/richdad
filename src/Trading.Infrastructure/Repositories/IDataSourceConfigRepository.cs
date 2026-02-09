using Trading.Infrastructure.Models;

namespace Trading.Infrastructure.Repositories;

/// <summary>
/// 数据源配置仓储接口
/// </summary>
public interface IDataSourceConfigRepository
{
    /// <summary>
    /// 获取当前数据源配置
    /// </summary>
    Task<DataSourceConfig> GetConfigAsync();

    /// <summary>
    /// 更新数据源配置
    /// </summary>
    Task UpdateConfigAsync(DataSourceConfig config);
}
