using Trading.Models;

namespace Trading.Core.Trading;

/// <summary>
/// 统一订单执行服务接口（支持多平台：Oanda、TradeLocker）
/// </summary>
public interface IOrderExecutionService
{
    /// <summary>
    /// 获取当前使用的交易平台名称
    /// </summary>
    string PlatformName { get; }

    /// <summary>
    /// 下市价单
    /// </summary>
    /// <param name="symbol">交易品种（如 XAU_USD, EUR_USD）</param>
    /// <param name="lots">手数</param>
    /// <param name="direction">交易方向</param>
    /// <param name="stopLoss">止损价格（可选）</param>
    /// <param name="takeProfit">止盈价格（可选）</param>
    /// <param name="comment">备注信息（可选）</param>
    /// <returns>订单执行结果</returns>
    Task<OrderResult> PlaceMarketOrderAsync(
        string symbol,
        decimal lots,
        OrderDirection direction,
        decimal? stopLoss = null,
        decimal? takeProfit = null,
        string? comment = null);

    /// <summary>
    /// 下限价单
    /// </summary>
    /// <param name="symbol">交易品种</param>
    /// <param name="lots">手数</param>
    /// <param name="direction">交易方向</param>
    /// <param name="limitPrice">限价</param>
    /// <param name="stopLoss">止损价格（可选）</param>
    /// <param name="takeProfit">止盈价格（可选）</param>
    /// <param name="comment">备注信息（可选）</param>
    /// <returns>订单执行结果</returns>
    Task<OrderResult> PlaceLimitOrderAsync(
        string symbol,
        decimal lots,
        OrderDirection direction,
        decimal limitPrice,
        decimal? stopLoss = null,
        decimal? takeProfit = null,
        string? comment = null);

    /// <summary>
    /// 获取订单状态
    /// </summary>
    /// <param name="orderId">订单ID</param>
    /// <returns>订单状态</returns>
    Task<OrderStatus> GetOrderStatusAsync(string orderId);

    /// <summary>
    /// 修改止损止盈
    /// </summary>
    /// <param name="orderId">订单ID</param>
    /// <param name="newStopLoss">新的止损价格（可选）</param>
    /// <param name="newTakeProfit">新的止盈价格（可选）</param>
    /// <returns>是否修改成功</returns>
    Task<bool> ModifyOrderAsync(
        string orderId,
        decimal? newStopLoss = null,
        decimal? newTakeProfit = null);

    /// <summary>
    /// 平仓
    /// </summary>
    /// <param name="orderId">订单ID</param>
    /// <param name="lots">平仓手数（可选，默认全部平仓）</param>
    /// <returns>是否平仓成功</returns>
    Task<bool> CloseOrderAsync(string orderId, decimal? lots = null);

    /// <summary>
    /// 获取当前持仓
    /// </summary>
    /// <param name="symbol">交易品种（可选，为空则获取所有持仓）</param>
    /// <returns>持仓列表</returns>
    Task<List<Position>> GetOpenPositionsAsync(string? symbol = null);
}

/// <summary>
/// 订单执行结果
/// </summary>
public class OrderResult
{
    public bool Success { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public decimal ExecutedPrice { get; set; }
    public decimal ExecutedLots { get; set; }
    public DateTime ExecutedTime { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
}

/// <summary>
/// 订单方向
/// </summary>
public enum OrderDirection
{
    Buy,    // 买入（做多）
    Sell    // 卖出（做空）
}

/// <summary>
/// 订单状态
/// </summary>
public class OrderStatus
{
    public string OrderId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public OrderDirection Direction { get; set; }
    public decimal Lots { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal? StopLoss { get; set; }
    public decimal? TakeProfit { get; set; }
    public decimal ProfitLoss { get; set; }
    public DateTime OpenTime { get; set; }
    public OrderState State { get; set; }
    public string? Comment { get; set; }
}

/// <summary>
/// 订单状态枚举
/// </summary>
public enum OrderState
{
    Pending,    // 挂单中
    Open,       // 已开仓
    Closed,     // 已平仓
    Cancelled   // 已取消
}

/// <summary>
/// 持仓信息
/// </summary>
public class Position
{
    public string PositionId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public OrderDirection Direction { get; set; }
    public decimal Lots { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal? StopLoss { get; set; }
    public decimal? TakeProfit { get; set; }
    public decimal ProfitLoss { get; set; }
    public DateTime OpenTime { get; set; }
    public string? Comment { get; set; }
}
