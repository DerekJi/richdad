using Microsoft.AspNetCore.Mvc;
using Trading.Infrastructure.AI.Services;
using Trading.Infrastructure.AI.Configuration;
using Microsoft.Extensions.Options;

namespace Trading.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeepSeekTestController : ControllerBase
{
    private readonly IDeepSeekService? _deepSeekService;
    private readonly IDualTierAIService? _dualTierService;
    private readonly DeepSeekSettings _settings;
    private readonly DualTierAISettings _dualTierSettings;
    private readonly ILogger<DeepSeekTestController> _logger;

    public DeepSeekTestController(
        IDeepSeekService? deepSeekService,
        IDualTierAIService? dualTierService,
        IOptions<DeepSeekSettings> settings,
        IOptions<DualTierAISettings> dualTierSettings,
        ILogger<DeepSeekTestController> logger)
    {
        _deepSeekService = deepSeekService;
        _dualTierService = dualTierService;
        _settings = settings.Value;
        _dualTierSettings = dualTierSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// 检查DeepSeek服务配置状态（不消耗token）
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var status = new
        {
            DeepSeek = new
            {
                Configured = _deepSeekService != null,
                Enabled = _settings.Enabled,
                Endpoint = MaskEndpoint(_settings.Endpoint),
                ModelName = _settings.ModelName,
                HasApiKey = !string.IsNullOrEmpty(_settings.ApiKey),
                ServiceInjected = _deepSeekService != null,
                Configuration = new
                {
                    _settings.MaxDailyRequests,
                    _settings.MonthlyBudgetLimit,
                    _settings.Temperature,
                    _settings.MaxTokens,
                    _settings.TimeoutSeconds,
                    _settings.CostPer1MInputTokens,
                    _settings.CostPer1MOutputTokens
                },
                Usage = _deepSeekService != null ? new
                {
                    TodayRequests = _deepSeekService.GetTodayUsageCount(),
                    EstimatedMonthlyCost = _deepSeekService.GetEstimatedMonthlyCost(),
                    IsRateLimited = _deepSeekService.IsRateLimitReached()
                } : null,
                Message = GetDeepSeekStatusMessage()
            },
            DualTierAI = new
            {
                Enabled = _dualTierSettings.Enabled,
                Provider = _dualTierSettings.Provider.ToString(),
                Tier1MinScore = _dualTierSettings.Tier1MinScore,
                ServiceInjected = _dualTierService != null,
                Message = GetDualTierStatusMessage()
            }
        };

        return Ok(status);
    }

    /// <summary>
    /// 简单测试DeepSeek连接（消耗少量token，约100 tokens）
    /// </summary>
    [HttpGet("test-connection")]
    public async Task<IActionResult> TestConnection()
    {
        if (_deepSeekService == null)
        {
            return BadRequest(new { Error = "DeepSeek服务未注入，请检查配置" });
        }

        if (!_settings.Enabled)
        {
            return BadRequest(new { Error = "DeepSeek服务未启用，请设置 DeepSeek:Enabled=true" });
        }

        try
        {
            _logger.LogInformation("开始测试DeepSeek连接...");

            var response = await _deepSeekService.ChatCompletionAsync(
                systemPrompt: "你是一个测试助手。",
                userMessage: "请回复：DeepSeek连接成功"
            );

            _logger.LogInformation("✅ DeepSeek连接测试成功");

            return Ok(new
            {
                Success = true,
                Message = "✅ DeepSeek连接测试成功",
                Response = response,
                TokensUsed = "约100 tokens",
                TodayUsage = _deepSeekService.GetTodayUsageCount(),
                EstimatedMonthlyCost = _deepSeekService.GetEstimatedMonthlyCost(),
                CostPerRequest = "$0.00014 (1000 tokens)"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ DeepSeek连接测试失败");

            return StatusCode(500, new
            {
                Success = false,
                Error = ex.Message,
                Details = ex.InnerException?.Message,
                Troubleshooting = new[]
                {
                    "1. 检查 Endpoint 是否正确: https://api.deepseek.com",
                    "2. 检查 ApiKey 是否有效（从 https://platform.deepseek.com/ 获取）",
                    "3. 检查网络连接是否正常",
                    "4. 确认DeepSeek服务状态",
                    "5. 查看详细错误信息"
                }
            });
        }
    }

    /// <summary>
    /// 测试双级AI分析（使用DeepSeek）
    /// </summary>
    [HttpPost("test-dual-tier")]
    public async Task<IActionResult> TestDualTier()
    {
        if (_dualTierService == null)
        {
            return BadRequest(new { Error = "双级AI服务未注入，请检查配置" });
        }

        if (!_dualTierSettings.Enabled)
        {
            return BadRequest(new { Error = "双级AI服务未启用，请设置 DualTierAI:Enabled=true" });
        }

        if (_dualTierSettings.Provider.ToString() != "DeepSeek")
        {
            return BadRequest(new
            {
                Error = $"双级AI当前使用 {_dualTierSettings.Provider}，不是DeepSeek",
                Hint = "请设置 DualTierAI:Provider=DeepSeek"
            });
        }

        try
        {
            _logger.LogInformation("开始测试DeepSeek双级AI分析...");

            var testMarketData = @"
=== XAU/USD M15 市场数据 ===
时间: 2026-02-08 10:00
开盘价: 2850.00
最高价: 2865.50
最低价: 2848.20
收盘价: 2862.30

技术指标:
- EMA20: 2845.00（价格在EMA上方）
- RSI: 68（接近超买）
- 成交量: 高于平均水平
- 趋势: 上升趋势

市场情绪:
- 美元指数走弱
- 地缘政治风险上升
- 避险需求增加
";

            var result = await _dualTierService.AnalyzeAsync(testMarketData, "XAUUSD");

            _logger.LogInformation("✅ DeepSeek双级AI分析测试成功");

            return Ok(new
            {
                Success = true,
                Message = "✅ DeepSeek双级AI分析测试成功",
                Provider = "DeepSeek",
                Result = new
                {
                    PassedTier1 = result.PassedTier1,
                    Tier1 = result.Tier1Result != null ? new
                    {
                        result.Tier1Result.OpportunityScore,
                        result.Tier1Result.Reasoning,
                        result.Tier1Result.TotalTokens,
                        result.Tier1Result.ProcessingTimeMs
                    } : null,
                    Tier2 = result.Tier2Result != null ? new
                    {
                        result.Tier2Result.Action,
                        result.Tier2Result.EntryPrice,
                        result.Tier2Result.StopLoss,
                        result.Tier2Result.TakeProfit,
                        result.Tier2Result.RiskRewardRatio,
                        result.Tier2Result.TotalTokens,
                        result.Tier2Result.ProcessingTimeMs
                    } : null,
                    Cost = new
                    {
                        TotalCostUsd = result.TotalCostUsd,
                        Tier1CostUsd = result.Tier1Result?.CostUsd,
                        Tier2CostUsd = result.Tier2Result?.CostUsd
                    },
                    Performance = new
                    {
                        TotalTimeMs = result.TotalProcessingTimeMs,
                        Tier1TimeMs = result.Tier1Result?.ProcessingTimeMs,
                        Tier2TimeMs = result.Tier2Result?.ProcessingTimeMs
                    }
                },
                Usage = new
                {
                    TodayUsage = _dualTierService.GetTodayUsageCount(),
                    EstimatedMonthlyCost = _dualTierService.GetEstimatedMonthlyCost()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ DeepSeek双级AI分析测试失败");

            return StatusCode(500, new
            {
                Success = false,
                Error = ex.Message,
                Details = ex.InnerException?.Message,
                Troubleshooting = new[]
                {
                    "1. 确认DeepSeek服务已启用 (DeepSeek:Enabled=true)",
                    "2. 确认双级AI使用DeepSeek (DualTierAI:Provider=DeepSeek)",
                    "3. 检查API Key是否有效",
                    "4. 查看详细错误日志"
                }
            });
        }
    }

    /// <summary>
    /// 获取DeepSeek使用统计（不消耗token）
    /// </summary>
    [HttpGet("usage")]
    public IActionResult GetUsage()
    {
        if (_deepSeekService == null)
        {
            return BadRequest(new { Error = "DeepSeek服务未注入" });
        }

        return Ok(new
        {
            TodayRequests = _deepSeekService.GetTodayUsageCount(),
            MaxDailyRequests = _settings.MaxDailyRequests,
            RemainingToday = Math.Max(0, _settings.MaxDailyRequests - _deepSeekService.GetTodayUsageCount()),
            IsRateLimited = _deepSeekService.IsRateLimitReached(),
            EstimatedMonthlyCost = _deepSeekService.GetEstimatedMonthlyCost(),
            MonthlyBudgetLimit = _settings.MonthlyBudgetLimit,
            Pricing = new
            {
                InputCostPer1M = $"${_settings.CostPer1MInputTokens}",
                OutputCostPer1M = $"${_settings.CostPer1MOutputTokens}",
                EstimatedPerRequest = "$0.00014 (假设1000 tokens)"
            },
            Date = DateTime.UtcNow
        });
    }

    private string MaskEndpoint(string endpoint)
    {
        if (string.IsNullOrEmpty(endpoint))
            return "未配置";

        try
        {
            var uri = new Uri(endpoint);
            return $"{uri.Scheme}://{uri.Host}";
        }
        catch
        {
            return endpoint;
        }
    }

    private string GetDeepSeekStatusMessage()
    {
        if (_deepSeekService == null)
        {
            return "❌ DeepSeek服务未注入，请检查服务注册";
        }

        if (!_settings.Enabled)
        {
            return "⚠️ DeepSeek服务已配置但未启用，设置 DeepSeek:Enabled=true 以启用";
        }

        if (string.IsNullOrEmpty(_settings.Endpoint))
        {
            return "❌ 缺少 Endpoint 配置";
        }

        if (string.IsNullOrEmpty(_settings.ApiKey))
        {
            return "❌ 缺少 ApiKey 配置";
        }

        if (string.IsNullOrEmpty(_settings.ModelName))
        {
            return "⚠️ 缺少 ModelName 配置";
        }

        return "✅ DeepSeek服务配置完整，可以使用 /api/deepseektest/test-connection 测试连接";
    }

    private string GetDualTierStatusMessage()
    {
        if (_dualTierService == null)
        {
            return "❌ 双级AI服务未注入";
        }

        if (!_dualTierSettings.Enabled)
        {
            return "⚠️ 双级AI服务未启用";
        }

        if (_dualTierSettings.Provider.ToString() == "DeepSeek")
        {
            return "✅ 双级AI已配置为使用DeepSeek";
        }

        return $"⚠️ 双级AI当前使用 {_dualTierSettings.Provider}，不是DeepSeek";
    }
}
