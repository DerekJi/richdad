using TradingBacktest.Data.Models;

namespace TradingBacktest.Core.Strategies;

/// <summary>
/// 交易策略接口
/// </summary>
public interface ITradingStrategy
{
    /// <summary>
    /// 策略名称
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 检查是否可以开多单
    /// </summary>
    /// <param name="current">当前K线</param>
    /// <param name="previous">前一根K线(可能是Pin Bar)</param>
    /// <param name="hasOpenPosition">是否已有持仓</param>
    /// <returns>是否可以开多单</returns>
    bool CanOpenLong(Candle current, Candle previous, bool hasOpenPosition);
    
    /// <summary>
    /// 检查是否可以开空单
    /// </summary>
    /// <param name="current">当前K线</param>
    /// <param name="previous">前一根K线(可能是Pin Bar)</param>
    /// <param name="hasOpenPosition">是否已有持仓</param>
    /// <returns>是否可以开空单</returns>
    bool CanOpenShort(Candle current, Candle previous, bool hasOpenPosition);
    
    /// <summary>
    /// 计算止损位
    /// </summary>
    /// <param name="pinbar">Pin Bar K线</param>
    /// <param name="direction">交易方向</param>
    /// <returns>止损价位</returns>
    decimal CalculateStopLoss(Candle pinbar, TradeDirection direction);
    
    /// <summary>
    /// 计算止盈位
    /// </summary>
    /// <param name="entryPrice">入场价格</param>
    /// <param name="stopLoss">止损价格</param>
    /// <param name="direction">交易方向</param>
    /// <returns>止盈价位</returns>
    decimal CalculateTakeProfit(decimal entryPrice, decimal stopLoss, TradeDirection direction);
}
