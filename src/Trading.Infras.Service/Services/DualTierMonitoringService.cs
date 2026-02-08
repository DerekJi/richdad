using Microsoft.Extensions.Logging;
using Trading.AI.Models;
using Trading.AI.Services;

namespace Trading.Infras.Service.Services;

/// <summary>
/// 双级AI监控服务实现
/// </summary>
public class DualTierMonitoringService : IDualTierMonitoringService
{
    private readonly ILogger<DualTierMonitoringService> _logger;
    private readonly IDualTierAIService _dualTierAI;

    // 今日统计
    private int _todayTier1Calls = 0;
    private int _todayTier2Calls = 0;
    private int _todayFiltered = 0;
    private decimal _todayCost = 0m;
    private DateTime _lastResetDate = DateTime.UtcNow.Date;
    private readonly object _statsLock = new();

    public DualTierMonitoringService(
        ILogger<DualTierMonitoringService> logger,
        IDualTierAIService dualTierAI)
    {
        _logger = logger;
        _dualTierAI = dualTierAI;

        _logger.LogInformation("✅ 双级AI监控服务已初始化 - 成本优化模式启用");
    }

    public async Task<DualTierAnalysisResult?> AnalyzeAndFilterAsync(
        string symbol,
        string timeFrame,
        string marketData,
        CancellationToken cancellationToken = default)
    {
        ResetDailyStatsIfNeeded();

        try
        {
            _logger.LogInformation("🔍 开始双级AI分析 - {Symbol} {TimeFrame}", symbol, timeFrame);

            var result = await _dualTierAI.AnalyzeAsync(marketData, symbol, cancellationToken);

            // 更新统计
            UpdateStats(result);

            // 记录详细日志
            LogAnalysisResult(symbol, timeFrame, result);

            // 只返回通过Tier2且建议入场的结果
            if (result.ShouldEnter)
            {
                _logger.LogInformation("✅ 双级分析通过，建议入场 - {Symbol} {TimeFrame}", symbol, timeFrame);
                return result;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 双级AI分析失败 - {Symbol} {TimeFrame}", symbol, timeFrame);
            throw;
        }
    }

    public (int tier1Calls, int tier2Calls, int filtered, decimal cost) GetTodayStats()
    {
        lock (_statsLock)
        {
            ResetDailyStatsIfNeeded();
            return (_todayTier1Calls, _todayTier2Calls, _todayFiltered, _todayCost);
        }
    }

    private void UpdateStats(DualTierAnalysisResult result)
    {
        lock (_statsLock)
        {
            _todayTier1Calls++;
            _todayCost += result.Tier1Result?.CostUsd ?? 0m;

            if (result.PassedTier1 && result.Tier2Result != null)
            {
                _todayTier2Calls++;
                _todayCost += result.Tier2Result.CostUsd;
            }
            else
            {
                _todayFiltered++;
            }
        }
    }

    private void LogAnalysisResult(string symbol, string timeFrame, DualTierAnalysisResult result)
    {
        if (result.Tier1Result == null) return;

        // 记录Tier1结果
        if (!result.PassedTier1)
        {
            _logger.LogInformation(
                "🚫 Tier1拦截 - {Symbol} {TimeFrame} | " +
                "评分: {Score}/100 | " +
                "趋势: {Trend} | " +
                "原因: {Reason} | " +
                "成本: ${Cost:F4} | " +
                "耗时: {Ms}ms",
                symbol, timeFrame,
                result.Tier1Result.OpportunityScore,
                result.Tier1Result.TrendDirection,
                result.Tier1Result.RejectionReason,
                result.Tier1Result.CostUsd,
                result.Tier1Result.ProcessingTimeMs);

            // 记录今日统计
            var stats = GetTodayStats();
            _logger.LogInformation(
                "📊 今日统计 - Tier1调用: {T1}, Tier2调用: {T2}, 拦截: {Filtered}, 成本: ${Cost:F2}",
                stats.tier1Calls, stats.tier2Calls, stats.filtered, stats.cost);

            return;
        }

        // 记录Tier2结果
        if (result.Tier2Result != null)
        {
            _logger.LogInformation(
                "✅ Tier2完成 - {Symbol} {TimeFrame} | " +
                "Tier1评分: {T1Score} | " +
                "动作: {Action} | " +
                "入场: {Entry} | " +
                "止损: {SL} | " +
                "止盈: {TP} | " +
                "风险: ${Risk:F2} | " +
                "RR比: {RR:F2} | " +
                "总成本: ${Cost:F4} | " +
                "总耗时: {Ms}ms",
                symbol, timeFrame,
                result.Tier1Result.OpportunityScore,
                result.Tier2Result.Action,
                result.Tier2Result.EntryPrice,
                result.Tier2Result.StopLoss,
                result.Tier2Result.TakeProfit,
                result.Tier2Result.RiskAmountUsd,
                result.Tier2Result.RiskRewardRatio,
                result.TotalCostUsd,
                result.TotalProcessingTimeMs);

            // 记录详细分析
            _logger.LogInformation(
                "📝 Tier2深度分析:\n" +
                "支撑位: {Support}\n" +
                "阻力位: {Resistance}\n" +
                "假突破风险: {StopRun}\n" +
                "多周期共振: {MTF}\n" +
                "推理: {Reasoning}",
                result.Tier2Result.SupportAnalysis,
                result.Tier2Result.ResistanceAnalysis,
                result.Tier2Result.StopRunRisk,
                result.Tier2Result.MultiTimeframeAnalysis,
                result.Tier2Result.Reasoning);

            // 记录今日统计
            var stats = GetTodayStats();
            _logger.LogInformation(
                "📊 今日统计 - Tier1调用: {T1}, Tier2调用: {T2}, 拦截: {Filtered}, 成本: ${Cost:F2}",
                stats.tier1Calls, stats.tier2Calls, stats.filtered, stats.cost);
        }
    }

    private void ResetDailyStatsIfNeeded()
    {
        lock (_statsLock)
        {
            var today = DateTime.UtcNow.Date;
            if (_lastResetDate < today)
            {
                _logger.LogInformation(
                    "📅 每日统计重置 - 昨日数据: Tier1={T1}, Tier2={T2}, 拦截={Filtered}, 成本=${Cost:F2}",
                    _todayTier1Calls, _todayTier2Calls, _todayFiltered, _todayCost);

                _todayTier1Calls = 0;
                _todayTier2Calls = 0;
                _todayFiltered = 0;
                _todayCost = 0m;
                _lastResetDate = today;
            }
        }
    }
}
