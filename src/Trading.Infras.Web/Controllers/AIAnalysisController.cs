using Microsoft.AspNetCore.Mvc;
using Trading.Infras.Data.Repositories;
using Trading.Infras.Data.Models;

namespace Trading.Infras.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AIAnalysisController : ControllerBase
{
    private readonly IAIAnalysisRepository _repository;
    private readonly ILogger<AIAnalysisController> _logger;

    public AIAnalysisController(
        IAIAnalysisRepository repository,
        ILogger<AIAnalysisController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// 获取最近的AI分析记录
    /// </summary>
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentAnalyses([FromQuery] int count = 50)
    {
        if (count <= 0 || count > 500)
        {
            return BadRequest(new { Error = "count必须在1-500之间" });
        }

        var analyses = await _repository.GetRecentAnalysesAsync(count);
        return Ok(new
        {
            Count = analyses.Count,
            Analyses = analyses.Select(a => new
            {
                a.Id,
                a.AnalysisType,
                a.Symbol,
                a.TimeFrame,
                a.AnalysisTime,
                a.IsSuccess,
                a.EstimatedTokens,
                a.ResponseTimeMs,
                a.FromCache,
                a.ErrorMessage,
                ParsedResultPreview = TruncateString(a.ParsedResult, 200)
            })
        });
    }

    /// <summary>
    /// 获取AI分析详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAnalysisDetail(string id)
    {
        var analyses = await _repository.GetRecentAnalysesAsync(1000);
        var analysis = analyses.FirstOrDefault(a => a.Id == id);

        if (analysis == null)
        {
            return NotFound(new { Error = "未找到指定的分析记录" });
        }

        return Ok(analysis);
    }

    /// <summary>
    /// 按品种查询AI分析记录
    /// </summary>
    [HttpGet("symbol/{symbol}")]
    public async Task<IActionResult> GetAnalysesBySymbol(
        string symbol,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var analyses = await _repository.GetAnalysesBySymbolAsync(symbol, startDate, endDate);

        return Ok(new
        {
            Symbol = symbol,
            Count = analyses.Count,
            StartDate = startDate,
            EndDate = endDate,
            Analyses = analyses.Select(a => new
            {
                a.Id,
                a.AnalysisType,
                a.TimeFrame,
                a.AnalysisTime,
                a.IsSuccess,
                a.EstimatedTokens,
                a.ResponseTimeMs,
                a.FromCache,
                ParsedResultPreview = TruncateString(a.ParsedResult, 200)
            })
        });
    }

    /// <summary>
    /// 按分析类型查询
    /// </summary>
    [HttpGet("type/{analysisType}")]
    public async Task<IActionResult> GetAnalysesByType(
        string analysisType,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var analyses = await _repository.GetAnalysesByTypeAsync(analysisType, startDate, endDate);

        return Ok(new
        {
            AnalysisType = analysisType,
            Count = analyses.Count,
            StartDate = startDate,
            EndDate = endDate,
            Analyses = analyses.Select(a => new
            {
                a.Id,
                a.Symbol,
                a.TimeFrame,
                a.AnalysisTime,
                a.IsSuccess,
                a.EstimatedTokens,
                a.ResponseTimeMs,
                a.FromCache,
                ParsedResultPreview = TruncateString(a.ParsedResult, 200)
            })
        });
    }

    /// <summary>
    /// 获取AI分析统计信息
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var stats = await _repository.GetStatisticsAsync(startDate, endDate);

        var successRate = stats.TotalAnalyses > 0
            ? (double)stats.SuccessfulAnalyses / stats.TotalAnalyses * 100
            : 0;

        var cacheHitRate = stats.TotalAnalyses > 0
            ? (double)stats.CachedAnalyses / stats.TotalAnalyses * 100
            : 0;

        var estimatedCost = stats.TotalEstimatedTokens * 0.000005m; // ~$5 per 1M tokens

        return Ok(new
        {
            Period = new { StartDate = startDate, EndDate = endDate },
            Overview = new
            {
                stats.TotalAnalyses,
                stats.SuccessfulAnalyses,
                stats.FailedAnalyses,
                SuccessRate = $"{successRate:F2}%",
                stats.CachedAnalyses,
                CacheHitRate = $"{cacheHitRate:F2}%"
            },
            Performance = new
            {
                stats.AverageResponseTimeMs,
                AverageResponseTimeSec = stats.AverageResponseTimeMs / 1000.0
            },
            TokenUsage = new
            {
                stats.TotalEstimatedTokens,
                EstimatedCostUSD = $"${estimatedCost:F4}"
            },
            ByType = stats.AnalysesByType.Select(kvp => new
            {
                Type = kvp.Key,
                Count = kvp.Value,
                Percentage = stats.TotalAnalyses > 0 ? $"{(double)kvp.Value / stats.TotalAnalyses * 100:F2}%" : "0%"
            }),
            BySymbol = stats.AnalysesBySymbol.OrderByDescending(kvp => kvp.Value).Select(kvp => new
            {
                Symbol = kvp.Key,
                Count = kvp.Value,
                Percentage = stats.TotalAnalyses > 0 ? $"{(double)kvp.Value / stats.TotalAnalyses * 100:F2}%" : "0%"
            })
        });
    }

    private static string TruncateString(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value.Substring(0, maxLength) + "...";
    }
}
