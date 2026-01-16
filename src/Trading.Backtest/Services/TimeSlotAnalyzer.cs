using System.Text;
using Trading.Data.Models;

namespace Trading.Backtest.Services;

/// <summary>
/// 时间段分析器 - 分析不同时间段的盈亏表现
/// </summary>
public static class TimeSlotAnalyzer
{
    public class TimeSlotResult
    {
        public string TimeSlot { get; set; } = string.Empty;
        public int TradeCount { get; set; }
        public decimal TotalProfitLoss { get; set; }
        public decimal AvgProfitLoss { get; set; }
        public int WinCount { get; set; }
        public decimal WinRate { get; set; }
    }

    /// <summary>
    /// 分析交易按时间段的盈亏情况（按小时统计）
    /// </summary>
    public static List<TimeSlotResult> AnalyzeByHourSlots(List<Trade> trades)
    {
        var results = new Dictionary<int, TimeSlotResult>();

        foreach (var trade in trades)
        {
            if (!trade.CloseTime.HasValue || !trade.ProfitLoss.HasValue)
                continue;

            var hour = trade.OpenTime.Hour;

            if (!results.ContainsKey(hour))
            {
                results[hour] = new TimeSlotResult
                {
                    TimeSlot = $"{hour:D2}:00-{(hour + 1) % 24:D2}:00"
                };
            }

            var slot = results[hour];
            slot.TradeCount++;
            slot.TotalProfitLoss += trade.ProfitLoss.Value;
            if (trade.ProfitLoss.Value > 0)
                slot.WinCount++;
        }

        // 计算平均值和胜率
        foreach (var slot in results.Values)
        {
            slot.AvgProfitLoss = slot.TradeCount > 0 ? slot.TotalProfitLoss / slot.TradeCount : 0;
            slot.WinRate = slot.TradeCount > 0 ? (decimal)slot.WinCount / slot.TradeCount * 100 : 0;
        }

        return results.Values.ToList();
    }

    /// <summary>
    /// 获取盈利最多的时间段TOP5
    /// </summary>
    public static List<TimeSlotResult> GetTopProfitableSlots(List<Trade> trades, int top = 5)
    {
        var slots = AnalyzeByHourSlots(trades);
        return slots
            .Where(s => s.TradeCount > 0)
            .OrderByDescending(s => s.TotalProfitLoss)
            .Take(top)
            .ToList();
    }

    /// <summary>
    /// 获取亏损最多的时间段TOP5
    /// </summary>
    public static List<TimeSlotResult> GetTopLossSlots(List<Trade> trades, int top = 5)
    {
        var slots = AnalyzeByHourSlots(trades);
        return slots
            .Where(s => s.TradeCount > 0)
            .OrderBy(s => s.TotalProfitLoss)
            .Take(top)
            .ToList();
    }

    /// <summary>
    /// 生成时间段分析报告文本
    /// </summary>
    public static string GenerateTimeSlotReportText(List<Trade> trades)
    {
        var sb = new StringBuilder();

        var topProfit = GetTopProfitableSlots(trades, 5);
        var topLoss = GetTopLossSlots(trades, 5);

        sb.AppendLine(new string('=', 80));
        sb.AppendLine("时间段盈亏分析");
        sb.AppendLine(new string('=', 80));

        // 盈利最多时间段TOP5
        sb.AppendLine("\n盈利最多时间段 TOP5:");
        sb.AppendLine(new string('-', 80));
        sb.AppendLine($"{"时间段",-15} {"交易数",8} {"总盈亏",12} {"平均盈亏",12} {"胜率",10}");
        sb.AppendLine(new string('-', 80));
        
        foreach (var slot in topProfit)
        {
            sb.AppendLine($"{slot.TimeSlot,-15} {slot.TradeCount,8} {slot.TotalProfitLoss,11:N2} " +
                         $"{slot.AvgProfitLoss,11:N2} {slot.WinRate,9:F2}%");
        }

        // 亏损最多时间段TOP5
        sb.AppendLine("\n亏损最多时间段 TOP5:");
        sb.AppendLine(new string('-', 80));
        sb.AppendLine($"{"时间段",-15} {"交易数",8} {"总盈亏",12} {"平均盈亏",12} {"胜率",10}");
        sb.AppendLine(new string('-', 80));
        
        foreach (var slot in topLoss)
        {
            sb.AppendLine($"{slot.TimeSlot,-15} {slot.TradeCount,8} {slot.TotalProfitLoss,11:N2} " +
                         $"{slot.AvgProfitLoss,11:N2} {slot.WinRate,9:F2}%");
        }

        return sb.ToString();
    }
}
