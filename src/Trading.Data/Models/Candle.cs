namespace Trading.Data.Models;

/// <summary>
/// K线数据模型
/// </summary>
public class Candle
{
    /// <summary>
    /// 日期时间
    /// </summary>
    public DateTime DateTime { get; set; }

    /// <summary>
    /// 开盘价
    /// </summary>
    public decimal Open { get; set; }

    /// <summary>
    /// 最高价
    /// </summary>
    public decimal High { get; set; }

    /// <summary>
    /// 最低价
    /// </summary>
    public decimal Low { get; set; }

    /// <summary>
    /// 收盘价
    /// </summary>
    public decimal Close { get; set; }

    /// <summary>
    /// 成交量
    /// </summary>
    public long TickVolume { get; set; }

    /// <summary>
    /// 点差
    /// </summary>
    public int Spread { get; set; }

    /// <summary>
    /// EMA指标字典 (周期 => 值)
    /// </summary>
    public Dictionary<int, decimal> EMA { get; set; } = new();

    /// <summary>
    /// ATR指标
    /// </summary>
    public decimal ATR { get; set; }

    /// <summary>
    /// ADX (Average Directional Index) - 趋势强度指标
    /// </summary>
    public decimal ADX { get; set; }

    /// <summary>
    /// 整根K线的长度
    /// </summary>
    public decimal TotalRange => High - Low;

    /// <summary>
    /// 实体部分的长度
    /// </summary>
    public decimal BodySize => Math.Abs(Close - Open);

    /// <summary>
    /// 是否为阳线
    /// </summary>
    public bool IsBullish => Close > Open;

    /// <summary>
    /// 是否为阴线
    /// </summary>
    public bool IsBearish => Close < Open;

    /// <summary>
    /// 上影线长度
    /// </summary>
    public decimal UpperWick => High - Math.Max(Open, Close);

    /// <summary>
    /// 下影线长度
    /// </summary>
    public decimal LowerWick => Math.Min(Open, Close) - Low;

    /// <summary>
    /// UTC时区小时数
    /// </summary>
    public int UtcHour => DateTime.Hour;
}
