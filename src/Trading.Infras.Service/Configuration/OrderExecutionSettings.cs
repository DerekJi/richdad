namespace Trading.Infras.Service.Configuration;

/// <summary>
/// 订单执行配置
/// </summary>
public class OrderExecutionSettings
{
    /// <summary>
    /// 使用的交易平台（Oanda, TradeLocker, Mock）
    /// </summary>
    public string Provider { get; set; } = "Mock";

    /// <summary>
    /// 是否启用真实交易（false时强制使用Mock）
    /// </summary>
    public bool EnableRealTrading { get; set; } = false;

    /// <summary>
    /// 下单超时时间（秒）
    /// </summary>
    public int OrderTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 是否允许修改订单
    /// </summary>
    public bool AllowOrderModification { get; set; } = true;

    /// <summary>
    /// 最大持仓数量
    /// </summary>
    public int MaxOpenPositions { get; set; } = 10;
}
