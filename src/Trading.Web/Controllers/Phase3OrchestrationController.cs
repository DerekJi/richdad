using Microsoft.AspNetCore.Mvc;
using Trading.Services.AI;

namespace Trading.Web.Controllers;

/// <summary>
/// Phase 3 四级 AI 服务测试控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class Phase3OrchestrationController : ControllerBase
{
    private readonly ILogger<Phase3OrchestrationController> _logger;
    private readonly TradingOrchestrationService _orchestrationService;
    private readonly L1_DailyAnalysisService _l1Service;
    private readonly L2_StructureAnalysisService _l2Service;
    private readonly L3_SignalMonitoringService _l3Service;

    public Phase3OrchestrationController(
        ILogger<Phase3OrchestrationController> logger,
        TradingOrchestrationService orchestrationService,
        L1_DailyAnalysisService l1Service,
        L2_StructureAnalysisService l2Service,
        L3_SignalMonitoringService l3Service)
    {
        _logger = logger;
        _orchestrationService = orchestrationService;
        _l1Service = l1Service;
        _l2Service = l2Service;
        _l3Service = l3Service;
    }

    /// <summary>
    /// 测试完整的四级分析流程
    /// GET /api/phase3orchestration/full?symbol=XAUUSD
    /// </summary>
    [HttpGet("full")]
    public async Task<IActionResult> RunFullAnalysis([FromQuery] string symbol = "XAUUSD")
    {
        try
        {
            _logger.LogInformation("🚀 开始完整四级分析 - {Symbol}", symbol);

            var startTime = DateTime.UtcNow;
            var context = await _orchestrationService.ExecuteFullAnalysisAsync(symbol);
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

            return Ok(new
            {
                success = true,
                symbol,
                elapsedMs = elapsed,
                context = new
                {
                    createdAt = context.CreatedAt,
                    symbol = context.Symbol,

                    // L1 结果
                    l1_DailyBias = new
                    {
                        direction = context.L1_DailyBias.Direction,
                        confidence = context.L1_DailyBias.Confidence,
                        trendType = context.L1_DailyBias.TrendType,
                        supportLevels = context.L1_DailyBias.SupportLevels,
                        resistanceLevels = context.L1_DailyBias.ResistanceLevels,
                        reasoning = context.L1_DailyBias.Reasoning,
                        analyzedAt = context.L1_DailyBias.AnalyzedAt
                    },

                    // L2 结果
                    l2_Structure = new
                    {
                        marketCycle = context.L2_Structure.MarketCycle,
                        status = context.L2_Structure.Status,
                        alignedWithD1 = context.L2_Structure.AlignedWithD1,
                        currentPhase = context.L2_Structure.CurrentPhase,
                        reasoning = context.L2_Structure.Reasoning,
                        analyzedAt = context.L2_Structure.AnalyzedAt
                    },

                    // L3 结果
                    l3_Signal = new
                    {
                        status = context.L3_Signal.Status,
                        setupType = context.L3_Signal.SetupType,
                        direction = context.L3_Signal.Direction,
                        entryPrice = context.L3_Signal.EntryPrice,
                        stopLoss = context.L3_Signal.StopLoss,
                        takeProfit = context.L3_Signal.TakeProfit,
                        riskRewardRatio = context.L3_Signal.RiskRewardRatio,
                        reasoning = context.L3_Signal.Reasoning,
                        detectedAt = context.L3_Signal.DetectedAt
                    },

                    // L4 结果（如果有）
                    l4_Decision = context.L4_Decision != null ? new
                    {
                        action = context.L4_Decision.Action,
                        direction = context.L4_Decision.Direction,
                        entryPrice = context.L4_Decision.EntryPrice,
                        stopLoss = context.L4_Decision.StopLoss,
                        takeProfit = context.L4_Decision.TakeProfit,
                        lotSize = context.L4_Decision.LotSize,
                        confidenceScore = context.L4_Decision.ConfidenceScore,
                        reasoning = context.L4_Decision.Reasoning,
                        thinkingProcess = context.L4_Decision.ThinkingProcess,
                        riskFactors = context.L4_Decision.RiskFactors,
                        totalRiskAmount = context.L4_Decision.TotalRiskAmount,
                        totalRewardAmount = context.L4_Decision.TotalRewardAmount,
                        decidedAt = context.L4_Decision.DecidedAt
                    } : null,

                    // 验证状态
                    validation = new
                    {
                        isL1Valid = context.IsL1Valid,
                        isL2Valid = context.IsL2Valid,
                        isL3Valid = context.IsL3Valid,
                        isFullyAligned = context.IsFullyAligned,
                        terminatedLevel = context.GetTerminatedLevel(),
                        terminationReason = context.GetTerminationReason()
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 完整分析失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 仅测试 L1 日线分析
    /// GET /api/phase3orchestration/l1?symbol=XAUUSD
    /// </summary>
    [HttpGet("l1")]
    public async Task<IActionResult> TestL1Analysis([FromQuery] string symbol = "XAUUSD")
    {
        try
        {
            var result = await _l1Service.AnalyzeDailyBiasAsync(symbol);
            return Ok(new
            {
                success = true,
                symbol,
                result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ L1 测试失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 测试 L1 + L2 分析
    /// GET /api/phase3orchestration/l2?symbol=XAUUSD
    /// </summary>
    [HttpGet("l2")]
    public async Task<IActionResult> TestL2Analysis([FromQuery] string symbol = "XAUUSD")
    {
        try
        {
            var l1 = await _l1Service.AnalyzeDailyBiasAsync(symbol);
            var l2 = await _l2Service.AnalyzeStructureAsync(symbol, l1);

            return Ok(new
            {
                success = true,
                symbol,
                l1,
                l2
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ L2 测试失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 测试 L1 + L2 + L3 分析
    /// GET /api/phase3orchestration/l3?symbol=XAUUSD
    /// </summary>
    [HttpGet("l3")]
    public async Task<IActionResult> TestL3Monitoring([FromQuery] string symbol = "XAUUSD")
    {
        try
        {
            var l1 = await _l1Service.AnalyzeDailyBiasAsync(symbol);
            var l2 = await _l2Service.AnalyzeStructureAsync(symbol, l1);
            var l3 = await _l3Service.MonitorSignalAsync(symbol, l1, l2);

            return Ok(new
            {
                success = true,
                symbol,
                l1,
                l2,
                l3
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ L3 测试失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 快速检查是否应该分析
    /// GET /api/phase3orchestration/should-analyze?symbol=XAUUSD
    /// </summary>
    [HttpGet("should-analyze")]
    public async Task<IActionResult> ShouldAnalyze([FromQuery] string symbol = "XAUUSD")
    {
        try
        {
            var shouldAnalyze = await _orchestrationService.ShouldAnalyzeAsync(symbol);
            return Ok(new
            {
                success = true,
                symbol,
                shouldAnalyze,
                message = shouldAnalyze
                    ? "✅ 满足分析条件，可以继续"
                    : "⏭️ 不满足条件，跳过分析"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 快速检查失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 清除所有缓存
    /// POST /api/phase3orchestration/clear-cache?symbol=XAUUSD
    /// </summary>
    [HttpPost("clear-cache")]
    public IActionResult ClearCache([FromQuery] string symbol = "XAUUSD")
    {
        try
        {
            _orchestrationService.ClearAllCache(symbol);
            return Ok(new
            {
                success = true,
                symbol,
                message = "✅ 缓存已清除"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 清除缓存失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
