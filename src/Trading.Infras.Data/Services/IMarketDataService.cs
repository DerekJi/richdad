using Trading.Infras.Data.Models;

namespace Trading.Infras.Data.Services;

/// <summary>
/// 统一的市场数据服务接口（自动路由到TradeLocker或OANDA）
/// </summary>
public interface IMarketDataService
{
    /// <summary>
    /// 测试连接
    /// </summary>
    Task<bool> ConnectAsync();

    /// <summary>
    /// 获取品种实时价格
    /// </summary>
    Task<SymbolPrice?> GetSymbolPriceAsync(string symbol);

    /// <summary>
    /// 获取历史K线数据
    /// </summary>
    Task<List<Candle>> GetHistoricalDataAsync(string symbol, string timeFrame, int count);

    /// <summary>
    /// 获取账户信息
    /// </summary>
    Task<AccountInfo?> GetAccountInfoAsync();

    /// <summary>
    /// 获取当前使用的数据源提供商
    /// </summary>
    string GetCurrentProvider();
}
