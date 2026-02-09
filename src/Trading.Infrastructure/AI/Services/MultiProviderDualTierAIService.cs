using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Trading.Infrastructure.AI.Configuration;
using Trading.Infrastructure.AI.Models;

namespace Trading.Infrastructure.AI.Services;

/// <summary>
/// 多提供商双级AI分析服务
/// </summary>
public class MultiProviderDualTierAIService : IDualTierAIService
{
    private readonly ILogger<MultiProviderDualTierAIService> _logger;
    private readonly DualTierAISettings _settings;
    private readonly IUnifiedAIClient _aiClient;
    private readonly Dictionary<string, int> _dailyUsage = new();
    private readonly Dictionary<string, decimal> _monthlyCost = new();
    private readonly object _usageLock = new();

    public MultiProviderDualTierAIService(
        ILogger<MultiProviderDualTierAIService> logger,
        IOptions<DualTierAISettings> settings,
        IUnifiedAIClient aiClient)
    {
        _logger = logger;
        _settings = settings.Value;
        _aiClient = aiClient;

        _logger.LogInformation("多提供商双级AI服务已初始化 - Provider: {Provider}, Tier1: {Tier1}, Tier2: {Tier2}",
            _settings.Provider, _settings.Tier1.DeploymentName, _settings.Tier2.DeploymentName);
    }

    public async Task<DualTierAnalysisResult> AnalyzeAsync(
        string marketData,
        string symbol,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            throw new InvalidOperationException("双级AI架构未启用");
        }

        var totalStopwatch = Stopwatch.StartNew();
        var result = new DualTierAnalysisResult();

        // 第一步：执行Tier1过滤
        _logger.LogInformation("开始执行Tier1过滤 - Symbol: {Symbol}, Provider: {Provider}",
            symbol, _settings.Provider);
        result.Tier1Result = await ExecuteTier1FilterAsync(marketData, symbol, cancellationToken);

        // 检查是否通过Tier1
        if (!result.PassedTier1)
        {
            _logger.LogInformation("Tier1过滤未通过 - Score: {Score}, Reason: {Reason}",
                result.Tier1Result.OpportunityScore,
                result.Tier1Result.RejectionReason);

            totalStopwatch.Stop();
            result.TotalProcessingTimeMs = totalStopwatch.ElapsedMilliseconds;
            result.TotalCostUsd = result.Tier1Result.CostUsd;
            return result;
        }

        // 第二步：执行Tier2深度分析
        _logger.LogInformation("Tier1通过（Score: {Score}），开始执行Tier2深度分析",
            result.Tier1Result.OpportunityScore);
        result.Tier2Result = await ExecuteTier2AnalysisAsync(marketData, symbol, result.Tier1Result, cancellationToken);

        totalStopwatch.Stop();
        result.TotalProcessingTimeMs = totalStopwatch.ElapsedMilliseconds;
        result.TotalCostUsd = result.Tier1Result.CostUsd + result.Tier2Result.CostUsd;

        _logger.LogInformation("双级分析完成 - Provider: {Provider}, 总耗时: {TotalMs}ms, 总成本: ${TotalCost:F4}, 建议: {Action}",
            _settings.Provider,
            result.TotalProcessingTimeMs,
            result.TotalCostUsd,
            result.Tier2Result.Action);

        return result;
    }

    public async Task<Tier1FilterResult> ExecuteTier1FilterAsync(
        string marketData,
        string symbol,
        CancellationToken cancellationToken = default)
    {
        if (IsRateLimitReached())
        {
            throw new InvalidOperationException($"已达到每日调用限制: {_settings.MaxDailyRequests}");
        }

        var stopwatch = Stopwatch.StartNew();
        var systemPrompt = BuildTier1SystemPrompt(symbol);
        var userMessage = BuildTier1UserMessage(marketData, symbol);

        try
        {
            _logger.LogDebug("发送Tier1请求 - Model: {Model}", _settings.Tier1.DeploymentName);

            var response = await _aiClient.CompleteChatAsync(
                systemPrompt,
                userMessage,
                _settings.Tier1.Temperature,
                _settings.Tier1.MaxTokens,
                _settings.Tier1.DeploymentName,
                cancellationToken);

            stopwatch.Stop();

            var tier1Result = ParseTier1Response(response.Content);

            // 设置处理时间和成本
            tier1Result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
            tier1Result.TotalTokens = response.TotalTokens;
            tier1Result.CostUsd = CalculateCost(
                response.InputTokens,
                response.OutputTokens,
                _settings.Tier1);

            // 记录使用量
            RecordUsage(tier1Result.CostUsd);

            // 如果未通过，设置拦截原因
            if (tier1Result.OpportunityScore < _settings.Tier1MinScore)
            {
                tier1Result.RejectionReason = DetermineRejectionReason(tier1Result);
            }

            _logger.LogInformation("Tier1完成 - Score: {Score}, Direction: {Direction}, Time: {Ms}ms, Cost: ${Cost:F4}",
                tier1Result.OpportunityScore,
                tier1Result.TrendDirection,
                tier1Result.ProcessingTimeMs,
                tier1Result.CostUsd);

            return tier1Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tier1执行失败");
            throw;
        }
    }

    public async Task<Tier2AnalysisResult> ExecuteTier2AnalysisAsync(
        string marketData,
        string symbol,
        Tier1FilterResult? tier1Result = null,
        CancellationToken cancellationToken = default)
    {
        if (IsRateLimitReached())
        {
            throw new InvalidOperationException($"已达到每日调用限制: {_settings.MaxDailyRequests}");
        }

        var stopwatch = Stopwatch.StartNew();
        var systemPrompt = BuildTier2SystemPrompt(symbol);
        var userMessage = BuildTier2UserMessage(marketData, symbol, tier1Result);

        try
        {
            _logger.LogDebug("发送Tier2请求 - Model: {Model}", _settings.Tier2.DeploymentName);

            var response = await _aiClient.CompleteChatAsync(
                systemPrompt,
                userMessage,
                _settings.Tier2.Temperature,
                _settings.Tier2.MaxTokens,
                _settings.Tier2.DeploymentName,
                cancellationToken);

            stopwatch.Stop();

            var tier2Result = ParseTier2Response(response.Content);

            // 设置处理时间和成本
            tier2Result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
            tier2Result.TotalTokens = response.TotalTokens;
            tier2Result.CostUsd = CalculateCost(
                response.InputTokens,
                response.OutputTokens,
                _settings.Tier2);

            // 记录使用量
            RecordUsage(tier2Result.CostUsd);

            _logger.LogInformation("Tier2完成 - Action: {Action}, Time: {Ms}ms, Cost: ${Cost:F4}",
                tier2Result.Action,
                tier2Result.ProcessingTimeMs,
                tier2Result.CostUsd);

            return tier2Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tier2执行失败");
            throw;
        }
    }

    public bool IsRateLimitReached()
    {
        lock (_usageLock)
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            if (_dailyUsage.TryGetValue(today, out var count))
            {
                return count >= _settings.MaxDailyRequests;
            }
            return false;
        }
    }

    public int GetTodayUsageCount()
    {
        lock (_usageLock)
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            return _dailyUsage.TryGetValue(today, out var count) ? count : 0;
        }
    }

    public decimal GetEstimatedMonthlyCost()
    {
        lock (_usageLock)
        {
            var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
            return _monthlyCost.TryGetValue(currentMonth, out var cost) ? cost : 0m;
        }
    }

    private void RecordUsage(decimal cost)
    {
        lock (_usageLock)
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");

            if (!_dailyUsage.ContainsKey(today))
                _dailyUsage[today] = 0;
            _dailyUsage[today]++;

            if (!_monthlyCost.ContainsKey(currentMonth))
                _monthlyCost[currentMonth] = 0m;
            _monthlyCost[currentMonth] += cost;

            CleanupOldUsageData();
        }
    }

    private void CleanupOldUsageData()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");
        var keysToRemove = _dailyUsage.Keys.Where(k => string.Compare(k, cutoffDate) < 0).ToList();
        foreach (var key in keysToRemove)
            _dailyUsage.Remove(key);

        var cutoffMonth = DateTime.UtcNow.AddMonths(-3).ToString("yyyy-MM");
        var monthsToRemove = _monthlyCost.Keys.Where(k => string.Compare(k, cutoffMonth) < 0).ToList();
        foreach (var key in monthsToRemove)
            _monthlyCost.Remove(key);
    }

    private decimal CalculateCost(int inputTokens, int outputTokens, TierModelSettings tierSettings)
    {
        return (inputTokens / 1_000_000m * tierSettings.CostPer1MInputTokens) +
               (outputTokens / 1_000_000m * tierSettings.CostPer1MOutputTokens);
    }

    // Prompt构建方法（与原DualTierAIService相同）
    private string BuildTier1SystemPrompt(string symbol) => $@"你是一个专业的交易机会快速筛选器。你的任务是快速评估{symbol}的交易机会质量。

返回JSON格式，包含：
- opportunityScore (0-100): 交易机会评分
- trendDirection (Bullish/Bearish/Neutral): 趋势方向
- confidence (Low/Medium/High): 信号置信度
- quickSummary: 30字以内的简要总结

评分标准：
- 70分以上：强烈信号，值得深度分析
- 50-69分：一般信号，可能值得关注
- 50分以下：弱信号或无机会

要快速、准确，重点关注明确的技术信号。";

    private string BuildTier1UserMessage(string marketData, string symbol) =>
        $"评估{symbol}的交易机会：\n\n{marketData}";

    private string BuildTier2SystemPrompt(string symbol) => $@"你是一个专业的交易分析师，专注于{symbol}市场。你需要提供详细、深入的交易建议。

返回JSON格式，包含：
- action (Strong Buy/Buy/Hold/Sell/Strong Sell): 交易建议
- confidence (0-100): 信心水平
- entryPoints: 建议入场点位数组
- stopLoss: 止损建议
- takeProfits: 止盈目标数组（可多个）
- riskRewardRatio: 风险回报比
- analysis: 详细分析说明
- keyFactors: 关键影响因素数组
- timeframe: 建议持仓时间框架
- riskLevel (Low/Medium/High): 风险等级

请提供专业、可执行的交易策略。";

    private string BuildTier2UserMessage(string marketData, string symbol, Tier1FilterResult? tier1Result)
    {
        var message = $"请深度分析{symbol}并提供交易建议：\n\n{marketData}";
        if (tier1Result != null && _settings.IncludeTier1SummaryInTier2)
        {
            message += $"\n\n初步筛选结果：{tier1Result.Reasoning} (评分: {tier1Result.OpportunityScore})";
        }
        return message;
    }

    private Tier1FilterResult ParseTier1Response(string jsonResponse)
    {
        try
        {
            var result = JsonSerializer.Deserialize<Tier1FilterResult>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return result ?? throw new InvalidOperationException("无法解析Tier1响应");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析Tier1响应失败: {Response}", jsonResponse);
            throw;
        }
    }

    private Tier2AnalysisResult ParseTier2Response(string jsonResponse)
    {
        try
        {
            var result = JsonSerializer.Deserialize<Tier2AnalysisResult>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return result ?? throw new InvalidOperationException("无法解析Tier2响应");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析Tier2响应失败: {Response}", jsonResponse);
            throw;
        }
    }

    private string DetermineRejectionReason(Tier1FilterResult result)
    {
        if (result.TrendDirection == "Neutral")
            return "无明确趋势方向";
        return $"机会评分过低({result.OpportunityScore}分，需要≥{_settings.Tier1MinScore}分)";
    }
}
