using Microsoft.Extensions.Logging;
using Trading.Models;

namespace Trading.Services.AI;

/// <summary>
/// 交易编排服务 - 协调四级 AI 决策流程
/// L1 (D1) → L2 (H1) → L3 (M5) → L4 (Final)
/// 实现早期终止机制以节省成本
/// </summary>
public class TradingOrchestrationService
{
    private readonly ILogger<TradingOrchestrationService> _logger;
    private readonly L1_DailyAnalysisService _l1Service;
    private readonly L2_StructureAnalysisService _l2Service;
    private readonly L3_SignalMonitoringService _l3Service;
    private readonly L4_FinalDecisionService _l4Service;

    public TradingOrchestrationService(
        ILogger<TradingOrchestrationService> logger,
        L1_DailyAnalysisService l1Service,
        L2_StructureAnalysisService l2Service,
        L3_SignalMonitoringService l3Service,
        L4_FinalDecisionService l4Service)
    {
        _logger = logger;
        _l1Service = l1Service;
        _l2Service = l2Service;
        _l3Service = l3Service;
        _l4Service = l4Service;

        _logger.LogInformation("✅ 交易编排服务已初始化 - 四级级联架构");
    }

    /// <summary>
    /// 执行完整的四级分析流程
    /// </summary>
    /// <param name="symbol">品种代码（如 XAUUSD）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>完整的交易上下文，包含所有层级的分析结果</returns>
    public async Task<TradingContext> ExecuteFullAnalysisAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🚀 开始四级分析 - {Symbol}", symbol);

        var context = new TradingContext
        {
            Symbol = symbol,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            // ========== L1: D1 战略分析 ==========
            _logger.LogInformation("📊 [L1] 分析 D1 日线...");
            context.L1_DailyBias = await _l1Service.AnalyzeDailyBiasAsync(symbol, cancellationToken);

            // 验证 L1 结果
            if (!context.IsL1Valid)
            {
                var reason = context.GetTerminationReason();
                _logger.LogWarning("⛔ [L1] 验证失败 - {Reason}", reason);
                _logger.LogInformation("💰 成本节省：跳过 L2/L3/L4 分析");
                return context;
            }

            _logger.LogInformation("✅ [L1] 通过 - {Direction} ({Confidence}%)",
                context.L1_DailyBias.Direction, context.L1_DailyBias.Confidence);

            // ========== L2: H1 结构分析 ==========
            _logger.LogInformation("🔍 [L2] 分析 H1 结构...");
            context.L2_Structure = await _l2Service.AnalyzeStructureAsync(
                symbol, context.L1_DailyBias, cancellationToken);

            // 验证 L2 结果
            if (!context.IsL2Valid)
            {
                var reason = context.GetTerminationReason();
                _logger.LogWarning("⛔ [L2] 验证失败 - {Reason}", reason);
                _logger.LogInformation("💰 成本节省：跳过 L3/L4 分析");
                return context;
            }

            _logger.LogInformation("✅ [L2] 通过 - {Cycle} ({Status}), 对齐: {Aligned}",
                context.L2_Structure.MarketCycle, 
                context.L2_Structure.Status,
                context.L2_Structure.AlignedWithD1);

            // ========== L3: M5 信号监控 ==========
            _logger.LogInformation("🎯 [L3] 监控 M5 信号...");
            context.L3_Signal = await _l3Service.MonitorSignalAsync(
                symbol, context.L1_DailyBias, context.L2_Structure, cancellationToken);

            // 验证 L3 结果
            if (!context.IsL3Valid)
            {
                var reason = context.GetTerminationReason();
                _logger.LogInformation("⏸️ [L3] {Reason} - 无需进入 L4", reason);
                _logger.LogInformation("💰 成本节省：跳过 L4 最终决策");
                return context;
            }

            _logger.LogWarning("🎯 [L3] 检测到信号 - {Setup} ({Direction}), RR: {RR:F2}",
                context.L3_Signal.SetupType, 
                context.L3_Signal.Direction,
                context.L3_Signal.RiskRewardRatio);

            // ========== L4: 最终决策（含思维链） ==========
            _logger.LogInformation("🤔 [L4] 最终决策思考中...");
            context.L4_Decision = await _l4Service.MakeFinalDecisionAsync(
                symbol, 
                context.L1_DailyBias, 
                context.L2_Structure, 
                context.L3_Signal,
                cancellationToken);

            // 输出决策结果
            if (context.L4_Decision.ShouldExecute)
            {
                _logger.LogWarning(
                    "🎉 [L4] 决定执行交易！\n" +
                    "   品种: {Symbol}\n" +
                    "   方向: {Direction}\n" +
                    "   入场: {Entry:F2}\n" +
                    "   止损: {SL:F2}\n" +
                    "   止盈: {TP:F2}\n" +
                    "   手数: {Lots}\n" +
                    "   风险: ${Risk:F2}\n" +
                    "   预期收益: ${Reward:F2}\n" +
                    "   风险回报比: {RR:F2}\n" +
                    "   置信度: {Confidence}%",
                    symbol,
                    context.L4_Decision.Direction,
                    context.L4_Decision.EntryPrice,
                    context.L4_Decision.StopLoss,
                    context.L4_Decision.TakeProfit,
                    context.L4_Decision.LotSize,
                    context.L4_Decision.TotalRiskAmount,
                    context.L4_Decision.TotalRewardAmount,
                    context.L4_Decision.RiskRewardRatio,
                    context.L4_Decision.ConfidenceScore);

                if (context.L4_Decision.RiskFactors.Count > 0)
                {
                    _logger.LogWarning("   ⚠️ 风险因素: {Factors}", 
                        string.Join(", ", context.L4_Decision.RiskFactors));
                }
            }
            else
            {
                _logger.LogInformation("⛔ [L4] 决定拒绝交易 - {Reasoning}",
                    context.L4_Decision.Reasoning);
            }

            _logger.LogInformation("✅ 四级分析完成 - {Symbol}, 总耗时: {Elapsed}ms",
                symbol, (DateTime.UtcNow - context.CreatedAt).TotalMilliseconds);

            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 四级分析失败 - {Symbol}", symbol);
            throw;
        }
    }

    /// <summary>
    /// 快速检查是否应该进行完整分析
    /// 用于决定是否启动完整的四级流程
    /// </summary>
    /// <param name="symbol">品种代码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否应该继续分析</returns>
    public async Task<bool> ShouldAnalyzeAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 仅执行 L1 分析（有缓存，成本低）
            var dailyBias = await _l1Service.AnalyzeDailyBiasAsync(symbol, cancellationToken);

            // 检查是否满足最低条件
            if (dailyBias.Direction == "Neutral" || dailyBias.Confidence < 60)
            {
                _logger.LogInformation("⏭️ 跳过完整分析 - {Symbol}: {Direction} ({Confidence}%)",
                    symbol, dailyBias.Direction, dailyBias.Confidence);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 快速检查失败 - {Symbol}", symbol);
            return false;
        }
    }

    /// <summary>
    /// 清除所有层级的缓存
    /// </summary>
    /// <param name="symbol">品种代码</param>
    public void ClearAllCache(string symbol)
    {
        _logger.LogInformation("🗑️ 清除所有缓存 - {Symbol}", symbol);
        _l1Service.ClearCache(symbol);
        _l2Service.ClearCache(symbol);
        _logger.LogInformation("✅ 缓存已清除");
    }
}
