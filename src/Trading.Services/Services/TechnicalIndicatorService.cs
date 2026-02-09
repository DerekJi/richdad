using Trading.Models;

namespace Trading.Services.Services;

/// <summary>
/// Al Brooks 技术指标计算服务
/// </summary>
/// <remarks>
/// 实现 Al Brooks 价格行为学理论所需的核心技术指标计算，包括：
/// - Body%（收盘位置）：衡量 K 线强弱的关键指标
/// - 实体大小百分比：识别 Doji、强弱棒
/// - 上下影线百分比：识别反转信号
/// - EMA 距离：判断趋势强度和回调机会
/// </remarks>
public class TechnicalIndicatorService
{
    /// <summary>
    /// 计算 Body%（收盘位置）
    /// 0.0 = 收在最低点，1.0 = 收在最高点
    /// Al Brooks: "收盘位置是最重要的指标之一"
    /// </summary>
    public double CalculateBodyPercent(Candle candle)
    {
        var range = (double)(candle.High - candle.Low);
        if (range == 0) return 0.5; // Doji - 收在中间

        return (double)(candle.Close - candle.Low) / range;
    }

    /// <summary>
    /// 计算收盘位置（别名，与 Body% 相同）
    /// </summary>
    public double CalculateClosePosition(Candle candle)
    {
        return CalculateBodyPercent(candle);
    }

    /// <summary>
    /// 计算与 EMA 的距离（以 Ticks 为单位）
    /// </summary>
    public double CalculateDistanceToEMA(decimal close, decimal ema, string symbol)
    {
        var tickSize = GetTickSize(symbol);
        return (double)((close - ema) / tickSize);
    }

    /// <summary>
    /// 计算 K 线范围（High - Low）
    /// </summary>
    public decimal CalculateRange(Candle candle)
    {
        return candle.High - candle.Low;
    }

    /// <summary>
    /// 计算实体大小百分比
    /// Al Brooks: "实体越大，动能越强"
    /// </summary>
    public double CalculateBodySizePercent(Candle candle)
    {
        var range = (double)(candle.High - candle.Low);
        if (range == 0) return 0;

        var bodySize = (double)Math.Abs(candle.Close - candle.Open);
        return bodySize / range;
    }

    /// <summary>
    /// 判断是否为 Doji（十字星）
    /// Al Brooks: "Doji 表示多空平衡，通常是趋势反转或延续的信号"
    /// </summary>
    public bool IsDoji(Candle candle, double threshold = 0.1)
    {
        return CalculateBodySizePercent(candle) < threshold;
    }

    /// <summary>
    /// 判断是否为大实体棒（Strong Body）
    /// </summary>
    public bool IsStrongBody(Candle candle, double threshold = 0.6)
    {
        return CalculateBodySizePercent(candle) > threshold;
    }

    /// <summary>
    /// 判断是否为看涨棒（Close > Open）
    /// </summary>
    public bool IsBullBar(Candle candle)
    {
        return candle.Close > candle.Open;
    }

    /// <summary>
    /// 判断是否为看跌棒（Close < Open）
    /// </summary>
    public bool IsBearBar(Candle candle)
    {
        return candle.Close < candle.Open;
    }

    /// <summary>
    /// 计算上影线百分比
    /// </summary>
    public double CalculateUpperTailPercent(Candle candle)
    {
        var range = (double)(candle.High - candle.Low);
        if (range == 0) return 0;

        var upperBody = (double)(candle.High - Math.Max(candle.Open, candle.Close));
        return upperBody / range;
    }

    /// <summary>
    /// 计算下影线百分比
    /// </summary>
    public double CalculateLowerTailPercent(Candle candle)
    {
        var range = (double)(candle.High - candle.Low);
        if (range == 0) return 0;

        var lowerBody = (double)(Math.Min(candle.Open, candle.Close) - candle.Low);
        return lowerBody / range;
    }

    /// <summary>
    /// 获取品种的 Tick 大小
    /// </summary>
    private decimal GetTickSize(string symbol)
    {
        return symbol switch
        {
            "XAUUSD" or "XAGUSD" => 0.01m,
            "EURUSD" or "AUDUSD" or "GBPUSD" or "NZDUSD" => 0.00001m,
            "USDJPY" => 0.001m,
            "USDCAD" or "USDCHF" => 0.00001m,
            _ => 0.00001m
        };
    }
}
