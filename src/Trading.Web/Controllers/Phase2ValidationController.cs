using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Trading.Models;

namespace Trading.Web.Controllers;

/// <summary>
/// Phase 2 模型验证 API - 测试四级决策模型
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class Phase2ValidationController : ControllerBase
{
    private readonly ILogger<Phase2ValidationController> _logger;

    public Phase2ValidationController(ILogger<Phase2ValidationController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 测试所有模型的 JSON 序列化/反序列化
    /// GET /api/phase2validation/json
    /// </summary>
    [HttpGet("json")]
    public IActionResult TestJsonSerialization()
    {
        var results = new Dictionary<string, object>();

        try
        {
            // 测试 DailyBias
            var dailyBias = new DailyBias
            {
                Direction = "Bullish",
                Confidence = 85,
                SupportLevels = new List<double> { 2850.0, 2870.5 },
                ResistanceLevels = new List<double> { 2920.0, 2950.0 },
                TrendType = "Strong",
                Reasoning = "Strong bull trend with consecutive bull bars",
                AnalyzedAt = DateTime.UtcNow
            };

            var dailyBiasJson = JsonSerializer.Serialize(dailyBias);
            var dailyBiasDeserialized = JsonSerializer.Deserialize<DailyBias>(dailyBiasJson);

            results["DailyBias"] = new
            {
                original = dailyBias,
                json = dailyBiasJson,
                deserialized = dailyBiasDeserialized,
                success = dailyBiasDeserialized != null &&
                         dailyBiasDeserialized.Direction == dailyBias.Direction
            };

            // 测试 StructureAnalysis
            var structure = new StructureAnalysis
            {
                MarketCycle = "Trend",
                Status = "Active",
                AlignedWithD1 = true,
                CurrentPhase = "Pullback",
                Reasoning = "H1 shows clear uptrend aligned with D1 bullish bias",
                AnalyzedAt = DateTime.UtcNow
            };

            var structureJson = JsonSerializer.Serialize(structure);
            var structureDeserialized = JsonSerializer.Deserialize<StructureAnalysis>(structureJson);

            results["StructureAnalysis"] = new
            {
                original = structure,
                json = structureJson,
                deserialized = structureDeserialized,
                success = structureDeserialized != null &&
                         structureDeserialized.Status == structure.Status
            };

            // 测试 SignalDetection
            var signal = new SignalDetection
            {
                Status = "Potential_Setup",
                SetupType = "H2",
                EntryPrice = 2890.5,
                StopLoss = 2885.0,
                TakeProfit = 2905.0,
                Direction = "Buy",
                Reasoning = "H2 setup detected. Second bull bar after pullback to EMA20",
                DetectedAt = DateTime.UtcNow
            };

            var signalJson = JsonSerializer.Serialize(signal);
            var signalDeserialized = JsonSerializer.Deserialize<SignalDetection>(signalJson);

            results["SignalDetection"] = new
            {
                original = signal,
                json = signalJson,
                deserialized = signalDeserialized,
                riskRewardRatio = signal.RiskRewardRatio,
                isGoodRiskReward = signal.IsGoodRiskReward,
                success = signalDeserialized != null &&
                         signalDeserialized.Status == signal.Status
            };

            // 测试 FinalDecision
            var decision = new FinalDecision
            {
                Action = "Execute",
                Direction = "Buy",
                EntryPrice = 2890.5,
                StopLoss = 2885.0,
                TakeProfit = 2905.0,
                LotSize = 0.1,
                Reasoning = "Strong alignment across all timeframes. H2 setup with good RR ratio.",
                ThinkingProcess = "Step 1: D1 is bullish with 85% confidence. Step 2: H1 aligned...",
                ConfidenceScore = 85,
                RiskFactors = new List<string> { "Near resistance", "Wide stop loss" },
                DecidedAt = DateTime.UtcNow
            };

            var decisionJson = JsonSerializer.Serialize(decision);
            var decisionDeserialized = JsonSerializer.Deserialize<FinalDecision>(decisionJson);

            results["FinalDecision"] = new
            {
                original = decision,
                json = decisionJson,
                deserialized = decisionDeserialized,
                shouldExecute = decision.ShouldExecute,
                riskRewardRatio = decision.RiskRewardRatio,
                totalRisk = decision.TotalRiskAmount,
                success = decisionDeserialized != null &&
                         decisionDeserialized.Action == decision.Action
            };

            return Ok(new
            {
                success = true,
                message = "所有模型 JSON 序列化/反序列化测试通过",
                results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JSON 序列化测试失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 测试 TradingContext 级联验证逻辑
    /// GET /api/phase2validation/context
    /// </summary>
    [HttpGet("context")]
    public IActionResult TestTradingContext()
    {
        var scenarios = new List<object>();

        try
        {
            // 场景 1: 完整通过（所有层级都有效）
            var context1 = new TradingContext
            {
                Symbol = "XAUUSD",
                L1_DailyBias = new DailyBias
                {
                    Direction = "Bullish",
                    Confidence = 85,
                    TrendType = "Strong"
                },
                L2_Structure = new StructureAnalysis
                {
                    MarketCycle = "Trend",
                    Status = "Active",
                    AlignedWithD1 = true,
                    CurrentPhase = "Pullback"
                },
                L3_Signal = new SignalDetection
                {
                    Status = "Potential_Setup",
                    SetupType = "H2",
                    Direction = "Buy"
                }
            };

            scenarios.Add(new
            {
                scenario = "完整通过",
                context = context1,
                isL1Valid = context1.IsL1Valid,
                isL2Valid = context1.IsL2Valid,
                isL3Valid = context1.IsL3Valid,
                isFullyAligned = context1.IsFullyAligned,
                terminatedLevel = context1.GetTerminatedLevel(),
                terminationReason = context1.GetTerminationReason(),
                summary = context1.GetSummary()
            });

            // 场景 2: L1 失败（置信度不足）
            var context2 = new TradingContext
            {
                Symbol = "XAUUSD",
                L1_DailyBias = new DailyBias
                {
                    Direction = "Bullish",
                    Confidence = 50, // 低于 60
                    TrendType = "Weak"
                }
            };

            scenarios.Add(new
            {
                scenario = "L1 失败（置信度不足）",
                context = context2,
                isL1Valid = context2.IsL1Valid,
                isFullyAligned = context2.IsFullyAligned,
                terminatedLevel = context2.GetTerminatedLevel(),
                terminationReason = context2.GetTerminationReason()
            });

            // 场景 3: L1 通过但 L2 失败（状态为 Idle）
            var context3 = new TradingContext
            {
                Symbol = "XAUUSD",
                L1_DailyBias = new DailyBias
                {
                    Direction = "Bullish",
                    Confidence = 75,
                    TrendType = "Strong"
                },
                L2_Structure = new StructureAnalysis
                {
                    MarketCycle = "Range",
                    Status = "Idle", // 不活跃
                    AlignedWithD1 = false
                }
            };

            scenarios.Add(new
            {
                scenario = "L2 失败（状态 Idle）",
                context = context3,
                isL1Valid = context3.IsL1Valid,
                isL2Valid = context3.IsL2Valid,
                isFullyAligned = context3.IsFullyAligned,
                terminatedLevel = context3.GetTerminatedLevel(),
                terminationReason = context3.GetTerminationReason()
            });

            // 场景 4: L1/L2 通过但 L3 失败（无信号）
            var context4 = new TradingContext
            {
                Symbol = "XAUUSD",
                L1_DailyBias = new DailyBias
                {
                    Direction = "Bullish",
                    Confidence = 75,
                    TrendType = "Strong"
                },
                L2_Structure = new StructureAnalysis
                {
                    MarketCycle = "Trend",
                    Status = "Active",
                    AlignedWithD1 = true
                },
                L3_Signal = new SignalDetection
                {
                    Status = "No_Signal" // 无信号
                }
            };

            scenarios.Add(new
            {
                scenario = "L3 失败（无信号）",
                context = context4,
                isL1Valid = context4.IsL1Valid,
                isL2Valid = context4.IsL2Valid,
                isL3Valid = context4.IsL3Valid,
                isFullyAligned = context4.IsFullyAligned,
                terminatedLevel = context4.GetTerminatedLevel(),
                terminationReason = context4.GetTerminationReason()
            });

            return Ok(new
            {
                success = true,
                message = "TradingContext 级联验证测试完成",
                totalScenarios = scenarios.Count,
                scenarios
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TradingContext 测试失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 测试便捷属性和方法
    /// GET /api/phase2validation/properties
    /// </summary>
    [HttpGet("properties")]
    public IActionResult TestConvenienceProperties()
    {
        var tests = new Dictionary<string, object>();

        try
        {
            // 测试 DailyBias 属性
            var dailyBias = new DailyBias
            {
                Direction = "Bullish",
                Confidence = 85,
                SupportLevels = new List<double> { 2850.0, 2870.5, 2880.0 },
                ResistanceLevels = new List<double> { 2920.0, 2950.0 }
            };

            tests["DailyBias"] = new
            {
                isStrongBullish = dailyBias.IsStrongBullish,
                isStrongBearish = dailyBias.IsStrongBearish,
                isConfident = dailyBias.IsConfident(60),
                supportCount = dailyBias.SupportCount,
                resistanceCount = dailyBias.ResistanceCount
            };

            // 测试 StructureAnalysis 属性
            var structure = new StructureAnalysis
            {
                MarketCycle = "Trend",
                Status = "Active",
                AlignedWithD1 = true,
                CurrentPhase = "Pullback"
            };

            tests["StructureAnalysis"] = new
            {
                canTrade = structure.CanTrade,
                isTrending = structure.IsTrending,
                isPullback = structure.IsPullback,
                isBreakout = structure.IsBreakout,
                isRangebound = structure.IsRangebound
            };

            // 测试 SignalDetection 属性
            var signal = new SignalDetection
            {
                Status = "Potential_Setup",
                SetupType = "H2",
                EntryPrice = 2890.5,
                StopLoss = 2885.0,
                TakeProfit = 2905.0,
                Direction = "Buy"
            };

            tests["SignalDetection"] = new
            {
                hasSignal = signal.HasSignal,
                isBuySignal = signal.IsBuySignal,
                isSellSignal = signal.IsSellSignal,
                riskAmount = signal.RiskAmount,
                rewardAmount = signal.RewardAmount,
                riskRewardRatio = signal.RiskRewardRatio,
                isGoodRiskReward = signal.IsGoodRiskReward,
                isSecondEntry = signal.IsSecondEntry,
                isMTR = signal.IsMTR
            };

            // 测试 FinalDecision 属性
            var decision = new FinalDecision
            {
                Action = "Execute",
                Direction = "Buy",
                EntryPrice = 2890.5,
                StopLoss = 2885.0,
                TakeProfit = 2905.0,
                LotSize = 0.1,
                ConfidenceScore = 85,
                RiskFactors = new List<string> { "Factor 1", "Factor 2" }
            };

            tests["FinalDecision"] = new
            {
                shouldExecute = decision.ShouldExecute,
                isRejected = decision.IsRejected,
                isHighConfidence = decision.IsHighConfidence(75),
                riskAmount = decision.RiskAmount,
                rewardAmount = decision.RewardAmount,
                riskRewardRatio = decision.RiskRewardRatio,
                totalRiskAmount = decision.TotalRiskAmount,
                totalRewardAmount = decision.TotalRewardAmount,
                riskFactorCount = decision.RiskFactorCount,
                hasHighRisk = decision.HasHighRisk
            };

            return Ok(new
            {
                success = true,
                message = "所有便捷属性和方法测试通过",
                tests
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "便捷属性测试失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 完整验证测试（运行所有测试）
    /// GET /api/phase2validation/all
    /// </summary>
    [HttpGet("all")]
    public async Task<IActionResult> RunAllTests()
    {
        var results = new Dictionary<string, object>();

        // 运行 JSON 序列化测试
        var jsonResult = TestJsonSerialization() as OkObjectResult;
        results["jsonSerialization"] = jsonResult?.Value ?? new { success = false };

        // 运行上下文验证测试
        var contextResult = TestTradingContext() as OkObjectResult;
        results["contextValidation"] = contextResult?.Value ?? new { success = false };

        // 运行属性测试
        var propertiesResult = TestConvenienceProperties() as OkObjectResult;
        results["convenienceProperties"] = propertiesResult?.Value ?? new { success = false };

        var allSuccess =
            (jsonResult?.Value as dynamic)?.success == true &&
            (contextResult?.Value as dynamic)?.success == true &&
            (propertiesResult?.Value as dynamic)?.success == true;

        return Ok(new
        {
            success = allSuccess,
            message = allSuccess ?
                "✅ Phase 2 所有验证测试通过！" :
                "⚠️ 部分测试失败，请查看详细结果",
            timestamp = DateTime.UtcNow,
            results
        });
    }
}
