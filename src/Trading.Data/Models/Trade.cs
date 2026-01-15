namespace Trading.Data.Models;

/// <summary>
/// 交易方向
/// </summary>
public enum TradeDirection
{
    Long,   // 多单
    Short   // 空单
}

/// <summary>
/// 平仓原因
/// </summary>
public enum CloseReason
{
    StopLoss,       // 止损
    TakeProfit,     // 止盈
    Manual          // 手动（暂未使用）
}

/// <summary>
/// 交易记录
/// </summary>
public class Trade
{
    /// <summary>
    /// 交易ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 交易方向
    /// </summary>
    public TradeDirection Direction { get; set; }
    
    /// <summary>
    /// 开仓时间
    /// </summary>
    public DateTime OpenTime { get; set; }
    
    /// <summary>
    /// 开仓点位
    /// </summary>
    public decimal OpenPrice { get; set; }
    
    /// <summary>
    /// 止损位
    /// </summary>
    public decimal StopLoss { get; set; }
    
    /// <summary>
    /// 止盈位
    /// </summary>
    public decimal TakeProfit { get; set; }
    
    /// <summary>
    /// 平仓时间
    /// </summary>
    public DateTime? CloseTime { get; set; }
    
    /// <summary>
    /// 平仓点位
    /// </summary>
    public decimal? ClosePrice { get; set; }
    
    /// <summary>
    /// 平仓原因
    /// </summary>
    public CloseReason? CloseReason { get; set; }
    
    /// <summary>
    /// 是否已平仓
    /// </summary>
    public bool IsClosed => CloseTime.HasValue;
    
    /// <summary>
    /// 止损点差
    /// </summary>
    public decimal StopLossPips => Direction == TradeDirection.Long 
        ? (OpenPrice - StopLoss) 
        : (StopLoss - OpenPrice);
    
    /// <summary>
    /// 止盈点差
    /// </summary>
    public decimal TakeProfitPips => Direction == TradeDirection.Long 
        ? (TakeProfit - OpenPrice) 
        : (OpenPrice - TakeProfit);
    
    /// <summary>
    /// 盈亏额 (点数)
    /// </summary>
    public decimal? ProfitLoss
    {
        get
        {
            if (!IsClosed) return null;
            return Direction == TradeDirection.Long
                ? (ClosePrice!.Value - OpenPrice)
                : (OpenPrice - ClosePrice!.Value);
        }
    }
    
    /// <summary>
    /// 收益率 (相对于风险)
    /// </summary>
    public decimal? ReturnRate
    {
        get
        {
            if (!IsClosed || StopLossPips == 0) return null;
            return ProfitLoss!.Value / StopLossPips;
        }
    }
    
    /// <summary>
    /// 是否盈利
    /// </summary>
    public bool? IsWinning => ProfitLoss > 0;
    
    /// <summary>
    /// 持仓时长
    /// </summary>
    public TimeSpan? HoldingDuration => IsClosed ? CloseTime!.Value - OpenTime : null;
}
