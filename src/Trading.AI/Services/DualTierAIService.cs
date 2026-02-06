using System.Diagnostics;
using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using Trading.AI.Configuration;
using Trading.AI.Models;

namespace Trading.AI.Services;

/// <summary>
/// 双级AI分析服务实现
/// </summary>
public class DualTierAIService : IDualTierAIService
{
    private readonly ILogger<DualTierAIService> _logger;
    private readonly DualTierAISettings _settings;
    private readonly AzureOpenAISettings _azureSettings;
    private readonly AzureOpenAIClient _client;
    private readonly Dictionary<string, int> _dailyUsage = new();
    private readonly Dictionary<string, decimal> _monthlyCost = new();
    private readonly object _usageLock = new();

    public DualTierAIService(
        ILogger<DualTierAIService> logger,
        IOptions<DualTierAISettings> settings,
        IOptions<AzureOpenAISettings> azureSettings)
    {
        _logger = logger;
        _settings = settings.Value;
        _azureSettings = azureSettings.Value;

        if (string.IsNullOrEmpty(_azureSettings.Endpoint) || string.IsNullOrEmpty(_azureSettings.ApiKey))
        {
            throw new InvalidOperationException("Azure OpenAI Endpoint 和 ApiKey 必须配置");
        }

        _client = new AzureOpenAIClient(
            new Uri(_azureSettings.Endpoint),
            new AzureKeyCredential(_azureSettings.ApiKey));

        _logger.LogInformation("双级AI服务已初始化 - Tier1: {Tier1Model}, Tier2: {Tier2Model}",
            _settings.Tier1.DeploymentName, _settings.Tier2.DeploymentName);
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
        _logger.LogInformation("开始执行Tier1过滤 - Symbol: {Symbol}", symbol);
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
        _logger.LogInformation("Tier1通过（Score: {Score}），开始执行Tier2深度分析", result.Tier1Result.OpportunityScore);
        result.Tier2Result = await ExecuteTier2AnalysisAsync(marketData, symbol, result.Tier1Result, cancellationToken);

        totalStopwatch.Stop();
        result.TotalProcessingTimeMs = totalStopwatch.ElapsedMilliseconds;
        result.TotalCostUsd = result.Tier1Result.CostUsd + result.Tier2Result.CostUsd;

        _logger.LogInformation("双级分析完成 - 总耗时: {TotalMs}ms, 总成本: ${TotalCost:F4}, 建议: {Action}",
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
        var chatClient = _client.GetChatClient(_settings.Tier1.DeploymentName);

        var systemPrompt = BuildTier1SystemPrompt(symbol);
        var userMessage = BuildTier1UserMessage(marketData, symbol);

        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userMessage)
            };

            var options = new ChatCompletionOptions
            {
                Temperature = _settings.Tier1.Temperature,
                MaxOutputTokenCount = _settings.Tier1.MaxTokens,
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            _logger.LogDebug("发送Tier1请求 - Model: {Model}", _settings.Tier1.DeploymentName);
            var response = await chatClient.CompleteChatAsync(messages, options, cancellationToken);

            stopwatch.Stop();

            if (response?.Value?.Content == null || response.Value.Content.Count == 0)
            {
                throw new InvalidOperationException("Tier1返回空响应");
            }

            var jsonResult = response.Value.Content[0].Text;
            var tier1Result = ParseTier1Response(jsonResult);

            // 设置处理时间和成本
            tier1Result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
            tier1Result.TotalTokens = response.Value.Usage.TotalTokenCount;
            tier1Result.CostUsd = CalculateCost(
                response.Value.Usage.InputTokenCount,
                response.Value.Usage.OutputTokenCount,
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
        var chatClient = _client.GetChatClient(_settings.Tier2.DeploymentName);

        var systemPrompt = BuildTier2SystemPrompt(symbol);
        var userMessage = BuildTier2UserMessage(marketData, symbol, tier1Result);

        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userMessage)
            };

            var options = new ChatCompletionOptions
            {
                Temperature = _settings.Tier2.Temperature,
                MaxOutputTokenCount = _settings.Tier2.MaxTokens,
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            _logger.LogDebug("发送Tier2请求 - Model: {Model}", _settings.Tier2.DeploymentName);
            var response = await chatClient.CompleteChatAsync(messages, options, cancellationToken);

            stopwatch.Stop();

            if (response?.Value?.Content == null || response.Value.Content.Count == 0)
            {
                throw new InvalidOperationException("Tier2返回空响应");
            }

            var jsonResult = response.Value.Content[0].Text;
            var tier2Result = ParseTier2Response(jsonResult);

            // 设置处理时间和成本
            tier2Result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
            tier2Result.TotalTokens = response.Value.Usage.TotalTokenCount;
            tier2Result.CostUsd = CalculateCost(
                response.Value.Usage.InputTokenCount,
                response.Value.Usage.OutputTokenCount,
                _settings.Tier2);

            // 保存Tier1总结
            if (tier1Result != null && _settings.IncludeTier1SummaryInTier2)
            {
                tier2Result.Tier1Summary = $"Score: {tier1Result.OpportunityScore}, Direction: {tier1Result.TrendDirection}";
            }

            // 记录使用量
            RecordUsage(tier2Result.CostUsd);

            _logger.LogInformation("Tier2完成 - Action: {Action}, Entry: {Entry}, SL: {SL}, TP: {TP}, Time: {Ms}ms, Cost: ${Cost:F4}",
                tier2Result.Action,
                tier2Result.EntryPrice,
                tier2Result.StopLoss,
                tier2Result.TakeProfit,
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
            return _dailyUsage.TryGetValue(today, out var count) && count >= _settings.MaxDailyRequests;
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

    private string BuildTier1SystemPrompt(string symbol)
    {
        return $@"你是一个专业的金融市场快速筛选专家。你的任务是快速评估 {symbol} 是否存在潜在交易机会。

**评估标准**：
1. 趋势强度：是否存在明显的趋势或即将形成趋势
2. 波动性：当前波动是否足够支持交易
3. 形态识别：是否存在关键的技术形态（如Pin Bar、突破、回调等）
4. 动能评估：价格动能是否充足

**输出要求**：
- 返回JSON格式
- OpportunityScore: 0-100的评分（70以上认为有机会）
- TrendDirection: ""Bullish"", ""Bearish"", 或 ""Neutral""
- Reasoning: 简短的评估理由（1-2句话）

**重要**：
- 你的目标是快速过滤掉震荡横盘时段，只有高质量机会才给出70+的评分
- 不确定时宁可保守，避免误报";
    }

    private string BuildTier1UserMessage(string marketData, string symbol)
    {
        return $@"请快速评估以下 {symbol} 市场数据是否存在交易机会：

{marketData}

返回JSON格式：
{{
  ""OpportunityScore"": 0-100,
  ""TrendDirection"": ""Bullish/Bearish/Neutral"",
  ""Reasoning"": ""简短理由""
}}";
    }

    private string BuildTier2SystemPrompt(string symbol)
    {
        return $@"你是一个专业的{symbol}交易策略分析师。当前{symbol}价格在4800+美元区域，你需要进行深度的风险回报分析。

**核心任务**：
1. 支撑/阻力位分析：识别关键价位强度
2. 假突破风险评估：评估Stop Run（诱多/诱空）的可能性
3. 多周期共振：检查不同时间周期的趋势一致性
4. 入场策略：确定最佳入场点、止损和止盈位置
5. 风险管理：确保单笔风险不超过$40

**输出要求**：
返回完整的JSON交易指令，包含：
- Action: ""BUY"", ""SELL"", 或 ""HOLD""
- EntryPrice, StopLoss, TakeProfit（如果Action不是HOLD）
- RiskAmountUsd, RiskRewardRatio
- 详细的Reasoning（包含支撑阻力分析、风险评估等）

**风险控制**：
- 单笔风险必须 <= $40
- 风险回报比建议 >= 2:1
- 优先考虑高概率、高质量的入场点";
    }

    private string BuildTier2UserMessage(string marketData, string symbol, Tier1FilterResult? tier1Result)
    {
        var tier1Context = tier1Result != null
            ? $@"
**Tier1快速评估结果**：
- 机会评分：{tier1Result.OpportunityScore}/100
- 趋势方向：{tier1Result.TrendDirection}
- 初步判断：{tier1Result.Reasoning}

"
            : "";

        return $@"{tier1Context}请对以下{symbol}市场数据进行深度分析并给出交易建议：

{marketData}

返回JSON格式：
{{
  ""Action"": ""BUY/SELL/HOLD"",
  ""EntryPrice"": 入场价,
  ""StopLoss"": 止损价,
  ""TakeProfit"": 止盈价,
  ""RiskAmountUsd"": 风险金额,
  ""RiskRewardRatio"": 风险回报比,
  ""SupportAnalysis"": ""支撑位分析"",
  ""ResistanceAnalysis"": ""阻力位分析"",
  ""StopRunRisk"": ""假突破风险评估"",
  ""MultiTimeframeAnalysis"": ""多周期共振分析"",
  ""Reasoning"": ""完整推理过程""
}}";
    }

    private Tier1FilterResult ParseTier1Response(string jsonResponse)
    {
        try
        {
            var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;

            return new Tier1FilterResult
            {
                OpportunityScore = root.GetProperty("OpportunityScore").GetInt32(),
                TrendDirection = root.GetProperty("TrendDirection").GetString() ?? "Neutral",
                Reasoning = root.GetProperty("Reasoning").GetString() ?? ""
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析Tier1响应失败: {Response}", jsonResponse);
            throw new InvalidOperationException("无法解析Tier1 AI响应", ex);
        }
    }

    private Tier2AnalysisResult ParseTier2Response(string jsonResponse)
    {
        try
        {
            var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;

            return new Tier2AnalysisResult
            {
                Action = root.GetProperty("Action").GetString() ?? "HOLD",
                EntryPrice = root.TryGetProperty("EntryPrice", out var entry) ? entry.GetDecimal() : null,
                StopLoss = root.TryGetProperty("StopLoss", out var sl) ? sl.GetDecimal() : null,
                TakeProfit = root.TryGetProperty("TakeProfit", out var tp) ? tp.GetDecimal() : null,
                RiskAmountUsd = root.TryGetProperty("RiskAmountUsd", out var risk) ? risk.GetDecimal() : null,
                RiskRewardRatio = root.TryGetProperty("RiskRewardRatio", out var rrr) ? rrr.GetDecimal() : null,
                SupportAnalysis = root.TryGetProperty("SupportAnalysis", out var support) ? support.GetString() : null,
                ResistanceAnalysis = root.TryGetProperty("ResistanceAnalysis", out var resistance) ? resistance.GetString() : null,
                StopRunRisk = root.TryGetProperty("StopRunRisk", out var stopRun) ? stopRun.GetString() : null,
                MultiTimeframeAnalysis = root.TryGetProperty("MultiTimeframeAnalysis", out var mtf) ? mtf.GetString() : null,
                Reasoning = root.GetProperty("Reasoning").GetString() ?? ""
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析Tier2响应失败: {Response}", jsonResponse);
            throw new InvalidOperationException("无法解析Tier2 AI响应", ex);
        }
    }

    private decimal CalculateCost(int inputTokens, int outputTokens, TierModelSettings modelSettings)
    {
        var inputCost = (decimal)inputTokens / 1_000_000m * modelSettings.CostPer1MInputTokens;
        var outputCost = (decimal)outputTokens / 1_000_000m * modelSettings.CostPer1MOutputTokens;
        return inputCost + outputCost;
    }

    private string DetermineRejectionReason(Tier1FilterResult result)
    {
        if (result.OpportunityScore < 30)
            return "市场横盘震荡，无明显趋势";
        if (result.OpportunityScore < 50)
            return "波动不足或形态不清晰";
        if (result.OpportunityScore < 70)
            return "机会质量一般，等待更好的入场点";

        return "未达到Tier1阈值";
    }

    private void RecordUsage(decimal cost)
    {
        lock (_usageLock)
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");

            // 记录每日调用次数
            if (!_dailyUsage.ContainsKey(today))
            {
                _dailyUsage[today] = 0;
            }
            _dailyUsage[today]++;

            // 记录每月成本
            if (!_monthlyCost.ContainsKey(currentMonth))
            {
                _monthlyCost[currentMonth] = 0m;
            }
            _monthlyCost[currentMonth] += cost;

            // 清理旧数据
            CleanupOldData();
        }
    }

    private void CleanupOldData()
    {
        var yesterday = DateTime.UtcNow.AddDays(-2).ToString("yyyy-MM-dd");
        var lastMonth = DateTime.UtcNow.AddMonths(-2).ToString("yyyy-MM");

        _dailyUsage.Keys.Where(k => string.Compare(k, yesterday) < 0).ToList()
            .ForEach(k => _dailyUsage.Remove(k));

        _monthlyCost.Keys.Where(k => string.Compare(k, lastMonth) < 0).ToList()
            .ForEach(k => _monthlyCost.Remove(k));
    }
}
