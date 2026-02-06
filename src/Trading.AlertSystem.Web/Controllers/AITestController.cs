using Microsoft.AspNetCore.Mvc;
using Trading.AI.Services;
using Trading.AI.Configuration;
using Microsoft.Extensions.Options;
using Trading.AlertSystem.Data.Repositories;
using Trading.AlertSystem.Data.Models;

namespace Trading.AlertSystem.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AITestController : ControllerBase
{
    private readonly IAzureOpenAIService? _openAIService;
    private readonly IMarketAnalysisService? _marketAnalysisService;
    private readonly IAIAnalysisRepository? _aiAnalysisRepository;
    private readonly AzureOpenAISettings _settings;
    private readonly ILogger<AITestController> _logger;

    public AITestController(
        IAzureOpenAIService? openAIService,
        IMarketAnalysisService? marketAnalysisService,
        IAIAnalysisRepository? aiAnalysisRepository,
        IOptions<AzureOpenAISettings> settings,
        ILogger<AITestController> logger)
    {
        _openAIService = openAIService;
        _marketAnalysisService = marketAnalysisService;
        _aiAnalysisRepository = aiAnalysisRepository;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// 检查AI服务配置状态（不消耗token）
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var status = new
        {
            Configured = _openAIService != null && _marketAnalysisService != null,
            Enabled = _settings.Enabled,
            Endpoint = MaskEndpoint(_settings.Endpoint),
            DeploymentName = _settings.DeploymentName,
            HasApiKey = !string.IsNullOrEmpty(_settings.ApiKey),
            ServiceInjected = new
            {
                OpenAIService = _openAIService != null,
                MarketAnalysisService = _marketAnalysisService != null
            },
            Configuration = new
            {
                _settings.MaxDailyRequests,
                _settings.MonthlyBudgetLimit,
                _settings.Temperature,
                _settings.MaxTokens,
                _settings.TimeoutSeconds
            },
            Usage = _openAIService != null ? new
            {
                TodayRequests = _openAIService.GetTodayUsageCount(),
                EstimatedMonthlyCost = _openAIService.GetEstimatedMonthlyCost(),
                IsRateLimited = _openAIService.IsRateLimitReached()
            } : null,
            Message = GetStatusMessage()
        };

        return Ok(status);
    }

    /// <summary>
    /// 简单测试AI连接（消耗少量token，约100 tokens）
    /// </summary>
    [HttpGet("test-connection")]
    public async Task<IActionResult> TestConnection()
    {
        if (_openAIService == null)
        {
            return BadRequest(new { Error = "AI服务未注入，请检查配置" });
        }

        if (!_settings.Enabled)
        {
            return BadRequest(new { Error = "AI服务未启用，请设置 AzureOpenAI:Enabled=true" });
        }

        try
        {
            _logger.LogInformation("开始测试Azure OpenAI连接...");

            var response = await _openAIService.ChatCompletionAsync(
                systemPrompt: "你是一个测试助手。",
                userMessage: "请回复：连接成功"
            );

            _logger.LogInformation("Azure OpenAI连接测试成功");

            return Ok(new
            {
                Success = true,
                Message = "AI连接测试成功",
                Response = response,
                TokensUsed = "约100 tokens",
                TodayUsage = _openAIService.GetTodayUsageCount(),
                EstimatedMonthlyCost = _openAIService.GetEstimatedMonthlyCost()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure OpenAI连接测试失败");

            return StatusCode(500, new
            {
                Success = false,
                Error = ex.Message,
                Details = ex.InnerException?.Message,
                Troubleshooting = new[]
                {
                    "1. 检查 Endpoint 是否正确",
                    "2. 检查 ApiKey 是否有效",
                    "3. 检查 DeploymentName 是否已在Azure创建",
                    "4. 确认Azure订阅有足够配额",
                    "5. 查看详细错误信息"
                }
            });
        }
    }

    /// <summary>
    /// 获取AI使用统计（不消耗token）
    /// </summary>
    [HttpGet("usage")]
    public IActionResult GetUsage()
    {
        if (_openAIService == null)
        {
            return BadRequest(new { Error = "AI服务未注入" });
        }

        return Ok(new
        {
            TodayRequests = _openAIService.GetTodayUsageCount(),
            MaxDailyRequests = _settings.MaxDailyRequests,
            RemainingToday = Math.Max(0, _settings.MaxDailyRequests - _openAIService.GetTodayUsageCount()),
            IsRateLimited = _openAIService.IsRateLimitReached(),
            EstimatedMonthlyCost = _openAIService.GetEstimatedMonthlyCost(),
            MonthlyBudgetLimit = _settings.MonthlyBudgetLimit,
            Date = DateTime.UtcNow
        });
    }

    private string MaskEndpoint(string endpoint)
    {
        if (string.IsNullOrEmpty(endpoint))
            return "未配置";

        // 只显示部分URL，隐藏敏感信息
        var uri = new Uri(endpoint);
        return $"{uri.Scheme}://{uri.Host.Substring(0, Math.Min(10, uri.Host.Length))}...";
    }

    private string GetStatusMessage()
    {
        if (_openAIService == null || _marketAnalysisService == null)
        {
            return "❌ AI服务未注入，请检查服务注册";
        }

        if (!_settings.Enabled)
        {
            return "⚠️ AI服务已配置但未启用，设置 AzureOpenAI:Enabled=true 以启用";
        }

        if (string.IsNullOrEmpty(_settings.Endpoint))
        {
            return "❌ 缺少 Endpoint 配置";
        }

        if (string.IsNullOrEmpty(_settings.ApiKey))
        {
            return "❌ 缺少 ApiKey 配置";
        }

        if (string.IsNullOrEmpty(_settings.DeploymentName))
        {
            return "⚠️ 缺少 DeploymentName 配置";
        }

        return "✅ AI服务配置完整，可以使用 /api/aitest/test-connection 测试连接";
    }

    /// <summary>
    /// 测试AI分析持久化功能（保存模拟数据到Cosmos DB）
    /// </summary>
    [HttpPost("test-persistence")]
    public async Task<IActionResult> TestPersistence()
    {
        if (_aiAnalysisRepository == null)
        {
            return BadRequest(new
            {
                Error = "AI分析仓储未注入，可能是Cosmos DB未配置",
                Hint = "检查 CosmosDb:ConnectionString 配置"
            });
        }

        try
        {
            _logger.LogInformation("开始测试AI分析持久化...");

            var testAnalysis = new AIAnalysisHistory
            {
                AnalysisType = "TestAnalysis",
                Symbol = "XAUUSD",
                TimeFrame = "M15",
                InputData = "{\"test\":\"input data\",\"symbol\":\"XAUUSD\",\"timestamp\":\"" + DateTime.UtcNow + "\"}",
                RawResponse = "This is a test AI response",
                ParsedResult = "{\"result\":\"success\",\"score\":85}",
                IsSuccess = true,
                EstimatedTokens = 150,
                ResponseTimeMs = 1234,
                FromCache = false
            };

            await _aiAnalysisRepository.SaveAnalysisAsync(testAnalysis);

            _logger.LogInformation("✅ AI分析记录保存成功: {Id}", testAnalysis.Id);

            // 验证是否能读取
            var recentAnalyses = await _aiAnalysisRepository.GetRecentAnalysesAsync(5);
            var savedRecord = recentAnalyses.FirstOrDefault(a => a.Id == testAnalysis.Id);

            if (savedRecord != null)
            {
                return Ok(new
                {
                    Success = true,
                    Message = "✅ AI分析持久化测试成功",
                    TestRecord = new
                    {
                        testAnalysis.Id,
                        testAnalysis.AnalysisType,
                        testAnalysis.Symbol,
                        testAnalysis.TimeFrame,
                        testAnalysis.IsSuccess,
                        testAnalysis.EstimatedTokens,
                        testAnalysis.CreatedAt
                    },
                    Verification = "已从数据库读取并验证记录存在",
                    RecentCount = recentAnalyses.Count,
                    Hint = "访问 /ai-analysis.html 查看记录"
                });
            }
            else
            {
                return Ok(new
                {
                    Success = true,
                    Message = "⚠️ 记录已保存但未能立即读取（可能是一致性延迟）",
                    TestRecord = new { testAnalysis.Id },
                    Hint = "稍后访问 /ai-analysis.html 查看记录"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ AI分析持久化测试失败");

            return StatusCode(500, new
            {
                Success = false,
                Error = ex.Message,
                InnerError = ex.InnerException?.Message,
                StackTrace = ex.StackTrace?.Split('\n').Take(5),
                Troubleshooting = new[]
                {
                    "1. 确认 Cosmos DB Emulator 已启动",
                    "2. 检查 ConnectionString 配置是否正确",
                    "3. 确认 AIAnalysisHistory 容器已创建",
                    "4. 查看详细错误信息"
                }
            });
        }
    }
}
