using Trading.Core.Indicators;
using Trading.Data.Models;

namespace Trading.Backtest.Console.Services;

/// <summary>
/// 策略诊断服务 - 分析为什么信号这么少
/// </summary>
public class StrategyDiagnosticService
{
    public class DiagnosticResult
    {
        public int TotalCandles { get; set; }
        public int CandlesWithValidEma { get; set; }
        public int CandlesInTradingHours { get; set; }
        public int PotentialPinBars { get; set; }
        public int BullishPinBars { get; set; }
        public int BearishPinBars { get; set; }
        public int PinBarsNearEma { get; set; }
        public int PinBarsAboveBaseEma { get; set; }
        public int PinBarsBelowBaseEma { get; set; }
        public int BreakoutConfirmations { get; set; }
        public int FinalSignals { get; set; }
        
        // 详细过滤统计
        public int FilteredByThreshold { get; set; }
        public int FilteredByBodySize { get; set; }
        public int FilteredByLongerWick { get; set; }
        public int FilteredByShorterWick { get; set; }
        public int FilteredByWickPercentage { get; set; }
        public int FilteredByAtrRatio { get; set; }
        public int FilteredByNotNearEma { get; set; }
        public int FilteredByBaseEmaPosition { get; set; }
        public int FilteredByNoBreakout { get; set; }
    }

    private readonly StrategyConfig _config;

    public StrategyDiagnosticService(StrategyConfig config)
    {
        _config = config;
    }

    public DiagnosticResult Analyze(List<Candle> candles)
    {
        var result = new DiagnosticResult
        {
            TotalCandles = candles.Count
        };

        for (int i = 1; i < candles.Count; i++)
        {
            var current = candles[i];
            var previous = candles[i - 1];

            // 检查EMA是否准备好
            var baseEma = IndicatorCalculator.GetEma(previous, _config.BaseEma);
            if (baseEma > 0)
            {
                result.CandlesWithValidEma++;
            }
            else
            {
                continue; // EMA未准备好，跳过
            }

            // 检查交易时间
            var hour = current.UtcHour;
            bool inTradingHours = hour >= _config.StartTradingHour && hour <= _config.EndTradingHour;
            if (inTradingHours)
            {
                result.CandlesInTradingHours++;
            }

            // 分析Pin Bar - 多单方向
            AnalyzePinBar(previous, current, baseEma, true, result);
            
            // 分析Pin Bar - 空单方向
            AnalyzePinBar(previous, current, baseEma, false, result);
        }

        return result;
    }

    private void AnalyzePinBar(Candle previous, Candle current, decimal baseEma, bool bullish, DiagnosticResult result)
    {
        var total = previous.TotalRange;
        
        // 1. 阈值过滤
        if (total < _config.Threshold)
        {
            result.FilteredByThreshold++;
            return;
        }

        var longerWick = bullish ? previous.LowerWick : previous.UpperWick;
        var shorterWick = bullish ? previous.UpperWick : previous.LowerWick;
        var body = previous.BodySize;

        // 2. ATR倍数过滤
        if (previous.ATR > 0 && longerWick < _config.MinLowerWickAtrRatio * previous.ATR)
        {
            result.FilteredByAtrRatio++;
            return;
        }

        // 3. 实体占比过滤
        if (total > 0 && body / total * 100 > _config.MaxBodyPercentage)
        {
            result.FilteredByBodySize++;
            return;
        }

        // 4. 长影线占比过滤
        if (total > 0 && longerWick / total * 100 < _config.MinLongerWickPercentage)
        {
            result.FilteredByWickPercentage++;
            return;
        }

        // 5. 短影线占比过滤
        if (total > 0 && shorterWick / total * 100 > _config.MaxShorterWickPercentage)
        {
            result.FilteredByShorterWick++;
            return;
        }

        // Pin Bar识别成功
        result.PotentialPinBars++;
        if (bullish)
            result.BullishPinBars++;
        else
            result.BearishPinBars++;

        // 6. 基准EMA位置过滤
        if (bullish && previous.Close <= baseEma)
        {
            result.FilteredByBaseEmaPosition++;
            return;
        }
        if (!bullish && previous.Close >= baseEma)
        {
            result.FilteredByBaseEmaPosition++;
            return;
        }

        if (bullish)
            result.PinBarsAboveBaseEma++;
        else
            result.PinBarsBelowBaseEma++;

        // 7. 靠近EMA过滤
        bool nearEma = IsNearAnyEma(previous, bullish);
        if (!nearEma)
        {
            result.FilteredByNotNearEma++;
            return;
        }

        result.PinBarsNearEma++;

        // 8. 突破确认过滤
        bool breakout = bullish 
            ? current.Close > previous.High 
            : current.Close < previous.Low;
        
        if (!breakout)
        {
            result.FilteredByNoBreakout++;
            return;
        }

        result.BreakoutConfirmations++;

        // 9. 检查交易时间
        var hour = current.UtcHour;
        bool inTradingHours = hour >= _config.StartTradingHour && hour <= _config.EndTradingHour;
        if (!inTradingHours)
        {
            return;
        }

        result.FinalSignals++;
    }

    private bool IsNearAnyEma(Candle candle, bool bullish)
    {
        foreach (var emaPeriod in _config.EmaList)
        {
            var emaValue = IndicatorCalculator.GetEma(candle, emaPeriod);
            if (emaValue == 0) continue;

            if (bullish)
            {
                var minBody = Math.Min(candle.Open, candle.Close);
                if (minBody > emaValue && 
                    (Math.Abs(candle.Low - emaValue) <= _config.NearEmaThreshold || candle.Low < emaValue))
                {
                    return true;
                }
            }
            else
            {
                var maxBody = Math.Max(candle.Open, candle.Close);
                if (maxBody < emaValue && 
                    (Math.Abs(candle.High - emaValue) <= _config.NearEmaThreshold || candle.High > emaValue))
                {
                    return true;
                }
            }
        }
        
        return false;
    }

    public void PrintDiagnostic(DiagnosticResult result)
    {
        var diagnosticText = GenerateDiagnosticText(result);
        System.Console.WriteLine(diagnosticText);
    }

    /// <summary>
    /// 生成诊断信息文本
    /// </summary>
    public string GenerateDiagnosticText(DiagnosticResult result)
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine("\n============================================================");
        sb.AppendLine("策略诊断分析");
        sb.AppendLine("============================================================");
        sb.AppendLine($"总K线数: {result.TotalCandles:N0}");
        sb.AppendLine($"EMA准备就绪的K线: {result.CandlesWithValidEma:N0} ({Percent(result.CandlesWithValidEma, result.TotalCandles)})");
        sb.AppendLine($"交易时段内的K线: {result.CandlesInTradingHours:N0} ({Percent(result.CandlesInTradingHours, result.TotalCandles)})");
        
        sb.AppendLine("\n--- Pin Bar识别统计 ---");
        sb.AppendLine($"潜在Pin Bar总数: {result.PotentialPinBars:N0}");
        sb.AppendLine($"  - 看涨Pin Bar: {result.BullishPinBars:N0}");
        sb.AppendLine($"  - 看跌Pin Bar: {result.BearishPinBars:N0}");
        
        sb.AppendLine("\n--- 过滤统计 (按优先级) ---");
        sb.AppendLine($"1. 波动太小(Threshold): {result.FilteredByThreshold:N0}");
        sb.AppendLine($"2. 影线长度不足(ATR倍数): {result.FilteredByAtrRatio:N0}");
        sb.AppendLine($"3. 实体太大(>{_config.MaxBodyPercentage}%): {result.FilteredByBodySize:N0}");
        sb.AppendLine($"4. 长影线占比不足(<{_config.MinLongerWickPercentage}%): {result.FilteredByWickPercentage:N0}");
        sb.AppendLine($"5. 短影线太长(>{_config.MaxShorterWickPercentage}%): {result.FilteredByShorterWick:N0}");
        sb.AppendLine($"6. EMA位置不符(多/空方向): {result.FilteredByBaseEmaPosition:N0}");
        sb.AppendLine($"7. 不靠近任何EMA: {result.FilteredByNotNearEma:N0}");
        sb.AppendLine($"8. 无突破确认: {result.FilteredByNoBreakout:N0}");
        
        sb.AppendLine("\n--- 通过各阶段的信号数 ---");
        sb.AppendLine($"Pin Bar识别: {result.PotentialPinBars:N0}");
        sb.AppendLine($"+ 在基准EMA正确一侧: {result.PinBarsAboveBaseEma + result.PinBarsBelowBaseEma:N0}");
        sb.AppendLine($"+ 靠近EMA: {result.PinBarsNearEma:N0}");
        sb.AppendLine($"+ 突破确认: {result.BreakoutConfirmations:N0}");
        sb.AppendLine($"+ 交易时段内: {result.FinalSignals:N0}");
        
        sb.AppendLine("\n--- 最大瓶颈分析 ---");
        var bottlenecks = new[]
        {
            ("波动太小", result.FilteredByThreshold),
            ("影线ATR倍数不足", result.FilteredByAtrRatio),
            ("长影线占比不足", result.FilteredByWickPercentage),
            ("不靠近EMA", result.FilteredByNotNearEma),
            ("无突破确认", result.FilteredByNoBreakout),
            ("EMA位置不符", result.FilteredByBaseEmaPosition)
        };
        
        foreach (var (name, count) in bottlenecks.OrderByDescending(x => x.Item2).Take(3))
        {
            sb.AppendLine($"  {name}: {count:N0}");
        }
        
        sb.AppendLine("\n--- 信号转化率 ---");
        sb.AppendLine($"K线 → Pin Bar: {Percent(result.PotentialPinBars, result.CandlesWithValidEma)}");
        sb.AppendLine($"Pin Bar → 靠近EMA: {Percent(result.PinBarsNearEma, result.PotentialPinBars)}");
        sb.AppendLine($"靠近EMA → 突破确认: {Percent(result.BreakoutConfirmations, result.PinBarsNearEma)}");
        sb.AppendLine($"突破确认 → 最终信号: {Percent(result.FinalSignals, result.BreakoutConfirmations)}");
        sb.AppendLine($"总转化率: {Percent(result.FinalSignals, result.CandlesWithValidEma)}");
        sb.AppendLine("============================================================\n");
        
        return sb.ToString();
    }

    private string Percent(int value, int total)
    {
        if (total == 0) return "0.00%";
        return $"{(double)value / total * 100:F2}%";
    }
}
