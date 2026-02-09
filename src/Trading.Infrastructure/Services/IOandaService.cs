using Trading.Infrastructure.Models;
using Trading.Models;

namespace Trading.Infrastructure.Services;

/// <summary>
/// OANDA API服务接口
/// </summary>
public interface IOandaService
{
    /// <summary>
    /// 测试API连接
    /// </summary>
    Task<bool> ConnectAsync();

    /// <summary>
    /// 获取品种实时价格
    /// </summary>
    Task<SymbolPrice?> GetSymbolPriceAsync(string symbol);

    /// <summary>
    /// 获取历史K线数据
    /// </summary>
    /// <param name="symbol">品种代码 (如 EUR_USD, XAU_USD)</param>
    /// <param name="timeFrame">时间周期 (M5, M15, H1, H4, D1)</param>
    /// <param name="count">获取的K线数量</param>
    Task<List<Trading.Models.Candle>> GetHistoricalDataAsync(string symbol, string timeFrame, int count);

    /// <summary>
    /// 获取账户信息
    /// </summary>
    Task<AccountInfo?> GetAccountInfoAsync();
}
