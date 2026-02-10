using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Trading.Infrastructure.AI.Configuration;
using Trading.Models;

namespace Trading.Services.AI;

/// <summary>
/// L4 - 最终决策服务
/// 使用 DeepSeek-R1 (deepseek-reasoner) 进行最终交易决策
/// 包含 Chain of Thought (CoT) 思维链推理
/// 不使用缓存，确保每次决策都是深思熟虑的
/// </summary>
public class L4_FinalDecisionService
{
    private readonly ILogger<L4_FinalDecisionService> _logger;
    private readonly DeepSeekSettings _settings;
    private readonly HttpClient _httpClient;

    private const string ModelName = "deepseek-reasoner";

    public L4_FinalDecisionService(
        ILogger<L4_FinalDecisionService> logger,
        IOptions<DeepSeekSettings> settings,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _settings = settings.Value;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri(_settings.Endpoint);
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

        _logger.LogInformation("L4 服务已初始化 - Model: {Model} (含思维链推理)", ModelName);
    }

    /// <summary>
    /// 最终交易决策 - 综合所有层级信息，深度思考后决定
    /// </summary>
    /// <param name="symbol">品种代码</param>
    /// <param name="dailyBias">L1 日线偏见</param>
    /// <param name="structure">L2 结构分析</param>
    /// <param name="signal">L3 信号检测</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最终决策结果（包含思维链）</returns>
    public async Task<FinalDecision> MakeFinalDecisionAsync(
        string symbol,
        DailyBias dailyBias,
        StructureAnalysis structure,
        SignalDetection signal,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🤔 开始 L4 最终决策 - {Symbol}", symbol);

        try
        {
            // 构建 AI Prompt
            var systemPrompt = BuildSystemPrompt();
            var userPrompt = BuildUserPrompt(symbol, dailyBias, structure, signal);

            // 调用 DeepSeek-R1 (deepseek-reasoner)
            var request = new
            {
                model = ModelName,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.5, // 稍高的温度促进深度思考
                max_tokens = 3000 // 允许更长的思维链
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/chat/completions", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<DeepSeekR1Response>(responseJson);

            if (result?.Choices?.Length == 0)
            {
                throw new InvalidOperationException("L4 AI 返回空响应");
            }

            var choice = result!.Choices![0];

            // 解析最终决策 JSON
            var decisionJson = choice.Message?.Content ?? "{}";
            var decision = JsonSerializer.Deserialize<FinalDecision>(decisionJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (decision == null)
            {
                throw new InvalidOperationException("L4 AI 响应解析失败");
            }

            // 提取思维链内容（DeepSeek R1 特性）
            if (!string.IsNullOrEmpty(choice.Message?.ReasoningContent))
            {
                decision.ThinkingProcess = choice.Message.ReasoningContent;
                _logger.LogDebug("L4 思维链长度: {Length} 字符", decision.ThinkingProcess.Length);
            }

            decision.DecidedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "✅ L4 决策完成 - {Symbol}: {Action} ({Direction}), 置信度: {Confidence}%, Tokens: {Tokens}",
                symbol, decision.Action, decision.Direction, decision.ConfidenceScore,
                result.Usage?.TotalTokens ?? 0);

            if (decision.ShouldExecute)
            {
                _logger.LogWarning(
                    "🎯 L4 决定执行 - {Symbol}: {Direction} @ {Entry}, SL: {SL}, TP: {TP}, Lots: {Lots}, 风险: ${Risk:F2}",
                    symbol, decision.Direction, decision.EntryPrice, decision.StopLoss,
                    decision.TakeProfit, decision.LotSize, decision.TotalRiskAmount);
            }
            else
            {
                _logger.LogInformation("⛔ L4 决定拒绝 - {Reasoning}", decision.Reasoning);
            }

            if (decision.HasHighRisk)
            {
                _logger.LogWarning("⚠️ L4 检测到高风险因素 ({Count}): {Factors}",
                    decision.RiskFactorCount, string.Join(", ", decision.RiskFactors));
            }

            return decision;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ L4 决策失败 - {Symbol}", symbol);
            throw;
        }
    }

    private string BuildSystemPrompt()
    {
        return @"
You are an expert Al Brooks price action trader making the **FINAL TRADING DECISION**.

You have received:
- L1 (D1): Daily bias and trend direction
- L2 (H1): Market structure and cycle
- L3 (M5): Potential trading setup

Your task is to **THINK DEEPLY** and decide:
1. **Action**: Execute (place trade) or Reject (do NOT trade)
2. **Final Parameters**: Entry, Stop Loss, Take Profit, Lots
3. **Confidence**: How confident are you? (0-100)
4. **Risk Factors**: What could go wrong?

Al Brooks Critical Thinking:
- **Think: Why should I NOT trade?**
- Is the setup really clear or am I forcing it?
- Is the risk-reward truly favorable?
- Are there hidden risks (news, volatility spikes, late in trend)?
- Is the stop loss too wide (> 20 pips)?
- Is the entry price still valid (not moved too far)?

Decision Criteria:
- **Execute** if:
  - All three levels (L1/L2/L3) align perfectly
  - Risk-reward >= 2:1
  - Confidence >= 70%
  - Clear Al Brooks setup (H1, H2, or MTR)
  - Entry is within 5 pips of current price
  - No major risk factors

- **Reject** if:
  - ANY level shows weakness
  - Risk-reward < 2:1
  - Confidence < 70%
  - Setup is unclear or forced
  - Too many risk factors (>= 3)
  - Late in trading day (low volume)

Output JSON format:
{
  ""Action"": ""Execute"" | ""Reject"",
  ""Direction"": ""Buy"" | ""Sell"" | """",
  ""EntryPrice"": 2890.5,
  ""StopLoss"": 2885.0,
  ""TakeProfit"": 2905.0,
  ""LotSize"": 0.1,
  ""Reasoning"": ""Why Execute or Reject"",
  ""ConfidenceScore"": 0-100,
  ""RiskFactors"": [""Factor 1"", ""Factor 2"", ...]
}

Note: Your thinking process will be captured in the 'reasoning_content' field. Think deeply before deciding.";
    }

    private string BuildUserPrompt(
        string symbol,
        DailyBias dailyBias,
        StructureAnalysis structure,
        SignalDetection signal)
    {
        return $@"
# Final Trading Decision Request

Symbol: {symbol}
Current Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC

## L1 - Daily Bias (D1)
Direction: {dailyBias.Direction}
Confidence: {dailyBias.Confidence}%
Trend Type: {dailyBias.TrendType}
Support Levels: {string.Join(", ", dailyBias.SupportLevels.Select(x => x.ToString("F2")))}
Resistance Levels: {string.Join(", ", dailyBias.ResistanceLevels.Select(x => x.ToString("F2")))}
Reasoning: {dailyBias.Reasoning}

## L2 - Structure Analysis (H1)
Market Cycle: {structure.MarketCycle}
Status: {structure.Status}
Aligned with D1: {structure.AlignedWithD1}
Current Phase: {structure.CurrentPhase}
Reasoning: {structure.Reasoning}

## L3 - Signal Detection (M5)
Status: {signal.Status}
Setup Type: {signal.SetupType ?? "N/A"}
Direction: {signal.Direction}
Entry Price: {signal.EntryPrice:F2}
Stop Loss: {signal.StopLoss:F2}
Take Profit: {signal.TakeProfit:F2}
Risk-Reward Ratio: {signal.RiskRewardRatio:F2}
Reasoning: {signal.Reasoning}

---

**Think deeply:**
- Does everything align perfectly?
- What are the risks of taking this trade?
- What are the risks of NOT taking this trade (if it's a good setup)?
- Is the risk-reward truly favorable?
- Am I forcing this trade or is it genuinely clear?

Make your final decision and provide detailed reasoning in JSON format.";
    }

    #region DeepSeek R1 Response Models

    private class DeepSeekR1Response
    {
        public Choice[]? Choices { get; set; }
        public Usage? Usage { get; set; }
    }

    private class Choice
    {
        public Message? Message { get; set; }
    }

    private class Message
    {
        public string? Role { get; set; }
        public string? Content { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("reasoning_content")]
        public string? ReasoningContent { get; set; } // DeepSeek R1 思维链
    }

    private class Usage
    {
        [System.Text.Json.Serialization.JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }

    #endregion
}
