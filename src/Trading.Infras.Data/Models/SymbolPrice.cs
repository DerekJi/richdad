namespace Trading.Infras.Data.Models;

/// <summary>
/// 交易品种实时价格数据
/// </summary>
public class SymbolPrice
{
    /// <summary>
    /// 交易品种代码
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// 买入价
    /// </summary>
    public decimal Bid { get; set; }

    /// <summary>
    /// 卖出价
    /// </summary>
    public decimal Ask { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 最后成交价
    /// </summary>
    public decimal LastPrice { get; set; }

    /// <summary>
    /// 当日最高价
    /// </summary>
    public decimal? High { get; set; }

    /// <summary>
    /// 当日最低价
    /// </summary>
    public decimal? Low { get; set; }
}
