using Trading.Infrastructure.Models;
using Trading.Models;

namespace Trading.Infrastructure.Repositories;

/// <summary>
/// K线数据仓储接口
/// </summary>
public interface ICandleRepository
{
    /// <summary>
    /// 获取指定时间范围的K线数据
    /// </summary>
    /// <param name="symbol">品种代码（如 XAUUSD）</param>
    /// <param name="timeFrame">时间周期（如 M5, H1）</param>
    /// <param name="startTime">开始时间（UTC）</param>
    /// <param name="endTime">结束时间（UTC）</param>
    /// <returns>K线列表</returns>
    Task<List<Candle>> GetRangeAsync(string symbol, string timeFrame, DateTime startTime, DateTime endTime);

    /// <summary>
    /// 批量保存K线数据
    /// </summary>
    /// <param name="symbol">品种代码</param>
    /// <param name="timeFrame">时间周期</param>
    /// <param name="candles">K线数据列表</param>
    /// <param name="source">数据源（默认 OANDA）</param>
    Task SaveBatchAsync(string symbol, string timeFrame, List<Candle> candles, string source = "OANDA");

    /// <summary>
    /// 获取最新K线时间
    /// </summary>
    /// <param name="symbol">品种代码</param>
    /// <param name="timeFrame">时间周期</param>
    /// <returns>最新时间，如果没有数据则返回 null</returns>
    Task<DateTime?> GetLatestTimeAsync(string symbol, string timeFrame);

    /// <summary>
    /// 获取最早K线时间
    /// </summary>
    /// <param name="symbol">品种代码</param>
    /// <param name="timeFrame">时间周期</param>
    /// <returns>最早时间，如果没有数据则返回 null</returns>
    Task<DateTime?> GetEarliestTimeAsync(string symbol, string timeFrame);

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    /// <returns>统计信息字典</returns>
    Task<Dictionary<string, object>> GetStatisticsAsync();

    /// <summary>
    /// 删除指定时间范围的数据
    /// </summary>
    /// <param name="symbol">品种代码</param>
    /// <param name="timeFrame">时间周期</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    Task DeleteRangeAsync(string symbol, string timeFrame, DateTime startTime, DateTime endTime);

    /// <summary>
    /// 检查指定时间是否存在数据
    /// </summary>
    /// <param name="symbol">品种代码</param>
    /// <param name="timeFrame">时间周期</param>
    /// <param name="time">时间点</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsAsync(string symbol, string timeFrame, DateTime time);
}
