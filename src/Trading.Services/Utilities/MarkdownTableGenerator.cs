using System.Text;
using Trading.Infrastructure.Models;
using Trading.Models;

namespace Trading.Services.Utilities;

/// <summary>
/// Markdown 表格生成器 - 为 AI Prompt 生成格式化的 Al Brooks 分析表格
/// </summary>
/// <remarks>
/// 生成符合 Al Brooks 价格行为分析要求的 Markdown 表格，包括：
/// - K 线基本信息（时间、OHLC）
/// - 技术指标（EMA20、Body%、Range 等）
/// - 形态标签（Inside、ii、Breakout 等）
/// - 信号标记
/// </remarks>
public class MarkdownTableGenerator
{
    /// <summary>
    /// 生成完整的 Markdown 表格（包含所有技术指标）
    /// </summary>
    /// <param name="data">预处理数据列表</param>
    /// <param name="symbol">品种代码</param>
    /// <param name="timeFrame">时间周期</param>
    /// <param name="includeHeader">是否包含表头</param>
    /// <returns>Markdown 表格字符串</returns>
    public string GenerateFullTable(
        List<ProcessedDataEntity> data,
        string symbol,
        string timeFrame,
        bool includeHeader = true)
    {
        var sb = new StringBuilder();

        if (includeHeader)
        {
            sb.AppendLine($"## {symbol} {timeFrame} - Al Brooks 价格行为分析数据");
            sb.AppendLine();
        }

        // 表头
        sb.AppendLine("| Bar | Time | Open | High | Low | Close | Range | Body% | EMA20 | Dist(T) | Tags | Sig |");
        sb.AppendLine("|-----|------|------|------|-----|-------|-------|-------|-------|---------|------|-----|");

        // 反向索引（最新的 K 线为 Bar 0）
        var count = data.Count;
        for (int i = 0; i < count; i++)
        {
            var item = data[i];
            var barIndex = i - count; // 例如：最后一根为 0，倒数第二根为 -1

            var tags = string.Join(", ", item.GetTags());
            var signalMark = item.IsSignalBar ? "✓" : "";

            sb.AppendLine(
                $"| {barIndex} | {item.Time:MM-dd HH:mm} | " +
                $"{item.Open:F2} | {item.High:F2} | {item.Low:F2} | {item.Close:F2} | " +
                $"{item.Range:F2} | {item.BodyPercent:P0} | {item.EMA20:F2} | " +
                $"{item.DistanceToEMA20:F1} | {tags} | {signalMark} |");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 生成精简的 Markdown 表格（仅关键指标，供 AI 快速分析）
    /// </summary>
    /// <param name="data">预处理数据列表</param>
    /// <param name="includeHeader">是否包含表头</param>
    /// <returns>Markdown 表格字符串</returns>
    public string GenerateCompactTable(
        List<ProcessedDataEntity> data,
        bool includeHeader = false)
    {
        var sb = new StringBuilder();

        if (includeHeader)
        {
            sb.AppendLine("### 关键 K 线数据（最近 30 根）");
            sb.AppendLine();
        }

        // 简化表头
        sb.AppendLine("| Bar | Time | Close | Body% | EMA20 | Dist | Tags |");
        sb.AppendLine("|-----|------|-------|-------|-------|------|------|");

        var count = data.Count;
        for (int i = 0; i < count; i++)
        {
            var item = data[i];
            var barIndex = i - count;

            var tags = string.Join(", ", item.GetTags());
            if (string.IsNullOrEmpty(tags)) tags = "-";

            // 标记重要 K 线（信号棒或有形态标签）
            var barLabel = item.IsSignalBar ? $"**{barIndex}**" : barIndex.ToString();

            sb.AppendLine(
                $"| {barLabel} | {item.Time:MM-dd HH:mm} | " +
                $"{item.Close:F2} | {item.BodyPercent:P0} | " +
                $"{item.EMA20:F2} | {item.DistanceToEMA20:F1} | {tags} |");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 从原始 K 线和指标数据生成 Markdown 表格
    /// （用于没有预处理数据的场景）
    /// </summary>
    /// <param name="candles">K 线数据</param>
    /// <param name="ema20Values">EMA20 值数组</param>
    /// <param name="patternTags">形态标签（索引 => 标签列表）</param>
    /// <param name="symbol">品种代码</param>
    /// <param name="timeFrame">时间周期</param>
    /// <returns>Markdown 表格字符串</returns>
    public string GenerateFromCandles(
        List<Candle> candles,
        decimal[] ema20Values,
        Dictionary<int, List<string>> patternTags,
        string symbol,
        string timeFrame)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"## {symbol} {timeFrame} - Market Data");
        sb.AppendLine();
        sb.AppendLine("| Bar | Time | Open | High | Low | Close | EMA20 | Tags |");
        sb.AppendLine("|-----|------|------|------|-----|-------|-------|------|");

        var count = candles.Count;
        for (int i = 0; i < count; i++)
        {
            var candle = candles[i];
            var barIndex = i - count;
            var ema20 = i < ema20Values.Length ? ema20Values[i] : 0;

            // 获取形态标签
            var tags = patternTags.ContainsKey(i)
                ? string.Join(", ", patternTags[i])
                : "-";

            sb.AppendLine(
                $"| {barIndex} | {candle.DateTime:MM-dd HH:mm} | " +
                $"{candle.Open:F2} | {candle.High:F2} | {candle.Low:F2} | {candle.Close:F2} | " +
                $"{ema20:F2} | {tags} |");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 生成形态摘要文本
    /// </summary>
    /// <param name="data">预处理数据列表</param>
    /// <returns>形态摘要文本</returns>
    public string GeneratePatternSummary(List<ProcessedDataEntity> data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Pattern Summary");
        sb.AppendLine();

        var count = data.Count;

        // 统计各类形态出现的位置
        var insideBarPositions = new List<int>();
        var iiPositions = new List<int>();
        var iiiPositions = new List<int>();
        var outsideBarPositions = new List<int>();
        var breakoutPositions = new List<int>();
        var signalBarPositions = new List<int>();
        var trendCountPositions = new List<(int bar, string tag)>();

        for (int i = 0; i < count; i++)
        {
            var item = data[i];
            var barIndex = i - count;
            var tags = item.GetTags();

            if (tags.Contains("Inside")) insideBarPositions.Add(barIndex);
            if (tags.Contains("ii")) iiPositions.Add(barIndex);
            if (tags.Contains("iii")) iiiPositions.Add(barIndex);
            if (tags.Contains("Outside")) outsideBarPositions.Add(barIndex);
            if (tags.Any(t => t.StartsWith("Breakout"))) breakoutPositions.Add(barIndex);
            if (item.IsSignalBar) signalBarPositions.Add(barIndex);

            // 趋势计数（H1-H9, L1-L9）
            var trendTags = tags.Where(t => t.StartsWith("H") || t.StartsWith("L"))
                                .Where(t => t.Length == 2 && char.IsDigit(t[1]))
                                .ToList();
            foreach (var tag in trendTags)
            {
                trendCountPositions.Add((barIndex, tag));
            }
        }

        // 输出摘要
        if (iiPositions.Any())
        {
            sb.AppendLine($"- **ii Structure** (Inside-Inside): Detected at Bar {string.Join(", ", iiPositions)}");
        }

        if (iiiPositions.Any())
        {
            sb.AppendLine($"- **iii Structure** (Triple Inside): Detected at Bar {string.Join(", ", iiiPositions)}");
        }

        if (outsideBarPositions.Any())
        {
            sb.AppendLine($"- **Outside Bar**: Detected at Bar {string.Join(", ", outsideBarPositions)}");
        }

        if (breakoutPositions.Any())
        {
            sb.AppendLine($"- **Breakout**: Detected at Bar {string.Join(", ", breakoutPositions)}");
        }

        if (trendCountPositions.Any())
        {
            var grouped = trendCountPositions.GroupBy(x => x.tag);
            foreach (var group in grouped)
            {
                var bars = string.Join(", ", group.Select(x => x.bar));
                sb.AppendLine($"- **{group.Key}** (Trend Count): Detected at Bar {bars}");
            }
        }

        if (signalBarPositions.Any())
        {
            sb.AppendLine($"- **Signal Bars**: Detected at Bar {string.Join(", ", signalBarPositions)}");
        }

        if (sb.Length == 22) // 只有标题，没有内容
        {
            sb.AppendLine("- No significant patterns detected in the recent bars.");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 生成市场状态摘要（供 AI 快速理解当前市场）
    /// </summary>
    /// <param name="data">预处理数据列表</param>
    /// <param name="symbol">品种代码</param>
    /// <returns>市场状态摘要</returns>
    public string GenerateMarketStateSummary(List<ProcessedDataEntity> data, string symbol)
    {
        if (!data.Any()) return "No data available.";

        var latest = data.Last();
        var ema20 = latest.EMA20;
        var currentPrice = latest.Close;
        var distance = latest.DistanceToEMA20;

        var sb = new StringBuilder();
        sb.AppendLine($"**{symbol} - Current Market State**");
        sb.AppendLine($"- Current Price: {currentPrice:F2}");
        sb.AppendLine($"- EMA20: {ema20:F2}");
        sb.AppendLine($"- Distance: {distance:F1} ticks ({(distance > 0 ? "Above" : "Below")} EMA)");

        // 判断趋势方向（简单判断：价格与 EMA 关系）
        var priceAboveEMA = currentPrice > ema20;
        var trend = priceAboveEMA ? "Bullish bias" : "Bearish bias";
        sb.AppendLine($"- Trend Bias: {trend}");

        // 最近波动性（最后 10 根 K 线平均 Range）
        var recentRanges = data.TakeLast(10).Select(d => d.Range).ToList();
        var avgRange = recentRanges.Any() ? recentRanges.Average() : 0;
        sb.AppendLine($"- Avg Range (Last 10 bars): {avgRange:F2}");

        return sb.ToString();
    }
}
