using Trading.AlertSystem.Data.Models;

namespace Trading.AlertSystem.Data.Services;

/// <summary>
/// TradeLocker数据服务接口
/// </summary>
public interface ITradeLockerService
{
    /// <summary>
    /// 连接到TradeLocker
    /// </summary>
    Task<bool> ConnectAsync();

    /// <summary>
    /// 获取指定品种的实时价格
    /// </summary>
    Task<SymbolPrice?> GetSymbolPriceAsync(string symbol);

    /// <summary>
    /// 获取多个品种的实时价格
    /// </summary>
    Task<IEnumerable<SymbolPrice>> GetSymbolPricesAsync(IEnumerable<string> symbols);

    /// <summary>
    /// 获取历史K线数据（用于计算指标）
    /// </summary>
    Task<IEnumerable<Candle>> GetHistoricalDataAsync(string symbol, string timeFrame, int bars);

    /// <summary>
    /// 获取账户信息
    /// </summary>
    Task<AccountInfo?> GetAccountInfoAsync();
}

/// <summary>
/// 账户信息
/// </summary>
public class AccountInfo
{
    public long AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal Equity { get; set; }
    public decimal Margin { get; set; }
    public decimal FreeMargin { get; set; }
    public string Currency { get; set; } = "USD";
}

/// <summary>
/// K线数据
/// </summary>
public class Candle
{
    public DateTime Time { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
}
