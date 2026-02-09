using Trading.Models;
using Trading.Backtest.Data.Models;

namespace Trading.Backtest.Data.Interfaces;

/// <summary>
/// 回测结果仓储接口
/// </summary>
public interface IBacktestRepository
{
    /// <summary>
    /// 保存回测结果
    /// </summary>
    Task<string> SaveBacktestResultAsync(BacktestResult result);

    /// <summary>
    /// 获取回测结果
    /// </summary>
    Task<BacktestResult?> GetBacktestResultAsync(string id);

    /// <summary>
    /// 获取所有回测结果
    /// </summary>
    Task<List<BacktestResult>> GetAllBacktestResultsAsync(string? symbol = null, int limit = 100);

    /// <summary>
    /// 删除回测结果
    /// </summary>
    Task DeleteBacktestResultAsync(string id);
}

/// <summary>
/// 策略配置仓储接口
/// </summary>
public interface IStrategyConfigRepository
{
    /// <summary>
    /// 保存策略配置
    /// </summary>
    Task<string> SaveConfigAsync(StrategyConfig config);

    /// <summary>
    /// 获取策略配置
    /// </summary>
    Task<StrategyConfig?> GetConfigAsync(string id);

    /// <summary>
    /// 获取所有配置
    /// </summary>
    Task<List<StrategyConfig>> GetAllConfigsAsync(string? symbol = null);
}
