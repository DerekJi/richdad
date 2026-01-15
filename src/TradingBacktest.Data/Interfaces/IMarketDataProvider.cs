using TradingBacktest.Data.Models;

namespace TradingBacktest.Data.Interfaces;

/// <summary>
/// 市场数据提供者接口
/// </summary>
public interface IMarketDataProvider
{
    /// <summary>
    /// 获取K线数据
    /// </summary>
    /// <param name="symbol">交易品种</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns>K线数据列表</returns>
    Task<List<Candle>> GetCandlesAsync(string symbol, DateTime? startTime = null, DateTime? endTime = null);
}
