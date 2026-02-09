using Trading.Infrastructure.AI.Models;
using CoreCandle = Trading.Models.Candle;

namespace Trading.Services.Services;

/// <summary>
/// 消息构建辅助类
/// </summary>
public static class TradingMessageBuilder
{
    /// <summary>
    /// 构建带有双级AI分析的交易信号消息
    /// </summary>
    public static string BuildDualTierSignalMessage(
        string symbol,
        string timeFrame,
        string direction,
        CoreCandle pinBarCandle,
        DualTierAnalysisResult dualTierResult)
    {
        if (dualTierResult.Tier2Result == null)
        {
            throw new ArgumentException("Tier2结果不能为空", nameof(dualTierResult));
        }

        var emoji = direction == "Long" ? "🟢" : "🔴";
        var directionCn = direction == "Long" ? "做多" : "做空";
        var tier2 = dualTierResult.Tier2Result;

        var message = $@"{emoji} **PinBar {directionCn}信号 [双级AI验证通过]**

**品种**: {symbol}
**周期**: {timeFrame}
**信号时间**: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC

📊 **AI推荐交易参数**:
• 入场价: {tier2.EntryPrice:F5}
• 止损价: {tier2.StopLoss:F5}
• 止盈价: {tier2.TakeProfit:F5}
• 风险金额: ${tier2.RiskAmountUsd:F2}
• 盈亏比: {tier2.RiskRewardRatio:F2}
• 建议手数: {tier2.LotSize:F2}

📍 **PinBar K线**:
• 时间: {pinBarCandle.DateTime:yyyy-MM-dd HH:mm}
• 开盘: {pinBarCandle.Open:F5}
• 最高: {pinBarCandle.High:F5}
• 最低: {pinBarCandle.Low:F5}
• 收盘: {pinBarCandle.Close:F5}

🤖 **Tier1快速评估** (GPT-4o-mini):
• 机会评分: {dualTierResult.Tier1Result?.OpportunityScore}/100
• 趋势方向: {dualTierResult.Tier1Result?.TrendDirection}
• 初步判断: {dualTierResult.Tier1Result?.Reasoning}
• 处理时间: {dualTierResult.Tier1Result?.ProcessingTimeMs}ms

🎯 **Tier2深度分析** (GPT-4o):
• 动作建议: {tier2.Action}
• 支撑位分析: {tier2.SupportAnalysis}
• 阻力位分析: {tier2.ResistanceAnalysis}
• 假突破风险: {tier2.StopRunRisk}
• 多周期共振: {tier2.MultiTimeframeAnalysis}

💡 **AI推理**:
{tier2.Reasoning}

📈 **性能指标**:
• 总处理时间: {dualTierResult.TotalProcessingTimeMs}ms
• 总成本: ${dualTierResult.TotalCostUsd:F4}

⚠️ **风险提示**:
• 本信号已通过双级AI验证（Tier1过滤 + Tier2深度分析）
• 单笔风险已控制在$40以内
• 请结合实际市场情况和资金管理进行决策！";

        return message;
    }

    /// <summary>
    /// 构建Tier1拦截的日志消息（不发送Telegram）
    /// </summary>
    public static string BuildTier1RejectionLog(
        string symbol,
        string timeFrame,
        Tier1FilterResult tier1Result)
    {
        return $@"🚫 Tier1拦截信号 - {symbol} {timeFrame}
评分: {tier1Result.OpportunityScore}/100 (阈值: 70)
趋势: {tier1Result.TrendDirection}
原因: {tier1Result.RejectionReason}
理由: {tier1Result.Reasoning}
成本节省: ${0.02m:F4} (未调用Tier2)";
    }
}
