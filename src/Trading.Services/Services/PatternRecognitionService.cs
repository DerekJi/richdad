using Trading.Models;
using Microsoft.Extensions.Logging;

namespace Trading.Services.Services;

/// <summary>
/// Al Brooks 形态识别服务
/// </summary>
/// <remarks>
/// 实现 Al Brooks 价格行为学的核心形态识别，包括：
/// - 内包线（ii/iii）：波动收缩，潜在突破信号
/// - 外包线（Outside Bar）：大幅波动，可能反转
/// - 突破（Breakout）：突破 20 根 K 线高低点
/// - 尖峰（Spike）：快速急涨急跌
/// - 跟进（Follow Through）：趋势延续确认
/// - 趋势计数（H1-H9/L1-L9）：识别回调入场点
/// - 信号棒（Signal Bar）：高概率入场信号
/// </remarks>
public class PatternRecognitionService
{
    private readonly TechnicalIndicatorService _indicatorService;
    private readonly ILogger<PatternRecognitionService> _logger;

    public PatternRecognitionService(
        TechnicalIndicatorService indicatorService,
        ILogger<PatternRecognitionService> logger)
    {
        _indicatorService = indicatorService;
        _logger = logger;
    }

    /// <summary>
    /// 识别所有形态并返回标签列表
    /// </summary>
    public List<string> RecognizePatterns(
        List<Candle> candles,
        int index,
        decimal ema20,
        string symbol)
    {
        var tags = new List<string>();

        if (index < 0 || index >= candles.Count)
            return tags;

        // 内包线形态
        if (IsInsideBar(candles, index))
        {
            tags.Add("Inside");

            // ii（连续两根内包线）
            if (index >= 1 && IsInsideBar(candles, index - 1))
            {
                tags.Add("ii");
            }

            // iii（连续三根内包线）
            if (index >= 2 &&
                IsInsideBar(candles, index - 1) &&
                IsInsideBar(candles, index - 2))
            {
                tags.Add("iii");
            }
        }

        // 外包线
        if (IsOutsideBar(candles, index))
        {
            tags.Add("Outside");
        }

        // 突破形态
        if (IsBreakoutBar(candles, index))
        {
            tags.Add("BO");
            var direction = _indicatorService.IsBullBar(candles[index]) ? "Bull" : "Bear";
            tags.Add($"BO_{direction}");
        }

        // Spike（强动能棒）
        if (IsSpike(candles, index))
        {
            tags.Add("Spike");
        }

        // 跟进棒（Follow Through）
        if (IsFollowThrough(candles, index))
        {
            tags.Add("FT");
            var strength = GetFollowThroughStrength(candles, index);
            tags.Add($"FT_{strength}");
        }

        // 测试 EMA20
        if (IsTestingEMA(candles[index], ema20))
        {
            tags.Add("Test_EMA20");
        }

        // EMA Gap Bar（整根 K 线在 EMA 一侧）
        if (IsEMAGapBar(candles[index], ema20))
        {
            var side = candles[index].Low > ema20 ? "Above" : "Below";
            tags.Add($"Gap_EMA_{side}");
        }

        // 趋势计数（H1/H2/L1/L2）
        var trendCount = GetTrendCount(candles, index, ema20);
        if (trendCount != null)
        {
            tags.Add(trendCount);
        }

        // Doji
        if (_indicatorService.IsDoji(candles[index]))
        {
            tags.Add("Doji");
        }

        // 信号棒
        if (IsSignalBar(candles, index, ema20))
        {
            tags.Add("Signal");
        }

        return tags;
    }

    /// <summary>
    /// 判断是否为内包线
    /// Al Brooks: "内包线表示波动收缩，通常是突破的前兆"
    /// </summary>
    private bool IsInsideBar(List<Candle> candles, int index)
    {
        if (index < 1) return false;

        var current = candles[index];
        var previous = candles[index - 1];

        return current.High < previous.High && current.Low > previous.Low;
    }

    /// <summary>
    /// 判断是否为外包线
    /// Al Brooks: "外包线表示波动扩大，可能是趋势反转或延续"
    /// </summary>
    private bool IsOutsideBar(List<Candle> candles, int index)
    {
        if (index < 1) return false;

        var current = candles[index];
        var previous = candles[index - 1];

        return current.High > previous.High && current.Low < previous.Low;
    }

    /// <summary>
    /// 判断是否为突破棒
    /// </summary>
    private bool IsBreakoutBar(List<Candle> candles, int index)
    {
        if (index < 20) return false;

        var current = candles[index];
        var recent = candles.Skip(index - 20).Take(20).ToList();

        var recentHigh = recent.Max(c => c.High);
        var recentLow = recent.Min(c => c.Low);

        // 突破最近 20 根 K 线的高低点
        var isBreakingHigh = current.Close > recentHigh;
        var isBreakingLow = current.Close < recentLow;

        // 实体大小大于平均波动的 1.5 倍
        var avgRange = recent.Average(c => c.High - c.Low);
        var currentRange = current.High - current.Low;
        var isStrongBody = currentRange > avgRange * 1.5m;

        return (isBreakingHigh || isBreakingLow) && isStrongBody;
    }

    /// <summary>
    /// 判断是否为 Spike（强动能棒）
    /// Al Brooks: "Spike 通常是趋势的高潮，可能是反转信号"
    /// </summary>
    private bool IsSpike(List<Candle> candles, int index)
    {
        if (index < 5) return false;

        var current = candles[index];
        var recent = candles.Skip(index - 5).Take(5).ToList();

        var avgRange = recent.Average(c => c.High - c.Low);
        var currentRange = current.High - current.Low;

        // 范围是平均值的 2 倍以上
        return currentRange > avgRange * 2.0m;
    }

    /// <summary>
    /// 判断是否为跟进棒（Follow Through）
    /// Al Brooks: "跟进棒确认突破有效性"
    /// </summary>
    private bool IsFollowThrough(List<Candle> candles, int index)
    {
        if (index < 2) return false;

        var current = candles[index];
        var previous = candles[index - 1];

        // 前一根是突破棒或强动能棒
        if (!IsBreakoutBar(candles, index - 1) && !IsSpike(candles, index - 1))
            return false;

        // 当前棒继续朝同方向收盘
        var prevDirection = _indicatorService.IsBullBar(previous);
        var currDirection = _indicatorService.IsBullBar(current);

        if (prevDirection != currDirection)
            return false;

        // 且收盘价继续创新高/新低
        if (prevDirection)
            return current.Close > previous.Close;
        else
            return current.Close < previous.Close;
    }

    /// <summary>
    /// 获取跟进棒强度
    /// </summary>
    private string GetFollowThroughStrength(List<Candle> candles, int index)
    {
        var bodyPercent = _indicatorService.CalculateBodySizePercent(candles[index]);

        return bodyPercent switch
        {
            > 0.7 => "Strong",
            > 0.4 => "Medium",
            _ => "Weak"
        };
    }

    /// <summary>
    /// 判断是否测试 EMA20
    /// Al Brooks: "EMA 测试是重要的入场机会"
    /// </summary>
    private bool IsTestingEMA(Candle candle, decimal ema20)
    {
        // K 线的影线触及 EMA20
        return candle.Low <= ema20 && candle.High >= ema20;
    }

    /// <summary>
    /// 判断是否为 EMA Gap Bar（整根 K 线在 EMA 一侧）
    /// </summary>
    private bool IsEMAGapBar(Candle candle, decimal ema20)
    {
        return candle.Low > ema20 || candle.High < ema20;
    }

    /// <summary>
    /// 获取趋势计数（H1/H2/L1/L2）
    /// Al Brooks: "H2/L2 是最佳入场点"
    /// </summary>
    private string? GetTrendCount(List<Candle> candles, int index, decimal ema20)
    {
        if (index < 5) return null;

        var current = candles[index];

        // 判断趋势方向（通过价格与 EMA 的关系）
        var isBullTrend = current.Close > ema20;

        if (isBullTrend)
        {
            // 多头趋势中，寻找 Higher High
            var count = 0;
            for (int i = index; i >= Math.Max(0, index - 10); i--)
            {
                if (i > 0 && candles[i].High > candles[i - 1].High)
                {
                    count++;

                    // 如果创出波段新高，计数重置
                    if (IsNewSwingHigh(candles, i))
                    {
                        count = 1;
                        break;
                    }
                }
            }

            return count > 0 ? $"H{Math.Min(count, 9)}" : null;
        }
        else
        {
            // 空头趋势中，寻找 Lower Low
            var count = 0;
            for (int i = index; i >= Math.Max(0, index - 10); i--)
            {
                if (i > 0 && candles[i].Low < candles[i - 1].Low)
                {
                    count++;

                    if (IsNewSwingLow(candles, i))
                    {
                        count = 1;
                        break;
                    }
                }
            }

            return count > 0 ? $"L{Math.Min(count, 9)}" : null;
        }
    }

    /// <summary>
    /// 判断是否创出波段新高
    /// </summary>
    private bool IsNewSwingHigh(List<Candle> candles, int index)
    {
        if (index < 10) return false;

        var current = candles[index];
        var recent = candles.Skip(index - 10).Take(10).ToList();

        return current.High > recent.Max(c => c.High);
    }

    /// <summary>
    /// 判断是否创出波段新低
    /// </summary>
    private bool IsNewSwingLow(List<Candle> candles, int index)
    {
        if (index < 10) return false;

        var current = candles[index];
        var recent = candles.Skip(index - 10).Take(10).ToList();

        return current.Low < recent.Min(c => c.Low);
    }

    /// <summary>
    /// 判断是否为信号棒
    /// Al Brooks: "信号棒是可以入场的 K 线"
    /// </summary>
    private bool IsSignalBar(List<Candle> candles, int index, decimal ema20)
    {
        var current = candles[index];
        var bodyPercent = _indicatorService.CalculateBodySizePercent(current);

        // 强收盘（Body% > 0.6）
        var hasStrongClose = bodyPercent > 0.6;

        // 在趋势方向上
        var closeAboveEMA = current.Close > ema20;
        var isClimaxBar = IsSpike(candles, index);

        // 信号棒：强收盘 + 在 EMA 正确一侧 + 非 Climax
        return hasStrongClose &&
               (closeAboveEMA == _indicatorService.IsBullBar(current)) &&
               !isClimaxBar;
    }

    /// <summary>
    /// 计算简单 EMA（用于趋势判断）
    /// </summary>
    public decimal CalculateEMA(List<Candle> candles, int period)
    {
        if (candles.Count < period)
            return 0;

        // 简化实现：使用 SMA 作为初始值
        var sma = candles.Take(period).Average(c => c.Close);
        var multiplier = 2.0m / (period + 1);

        var ema = sma;
        foreach (var candle in candles.Skip(period))
        {
            ema = (candle.Close - ema) * multiplier + ema;
        }

        return ema;
    }
}
