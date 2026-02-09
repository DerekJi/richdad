namespace Trading.Web.Controllers;

/// <summary>
/// 初始化历史数据请求
/// </summary>
public class InitializeDataRequest
{
    /// <summary>
    /// 品种代码（如 XAUUSD）
    /// </summary>
    public string? Symbol { get; set; }

    /// <summary>
    /// 时间周期（如 M5, H1）
    /// </summary>
    public string? TimeFrame { get; set; }

    /// <summary>
    /// K线数量（优先使用，适用于所有周期）
    /// </summary>
    public int? Count { get; set; }

    /// <summary>
    /// 初始化天数（仅当Count为空时使用，主要用于D1日线）
    /// </summary>
    public int? Days { get; set; }

    /// <summary>
    /// 品种列表（支持批量初始化）
    /// </summary>
    public List<string>? Symbols { get; set; }

    /// <summary>
    /// 时间周期列表（支持批量初始化）
    /// </summary>
    public List<string>? TimeFrames { get; set; }
}
