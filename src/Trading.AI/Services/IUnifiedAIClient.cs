namespace Trading.AI.Services;

/// <summary>
/// 统一AI客户端接口（支持多个提供商）
/// </summary>
public interface IUnifiedAIClient
{
    /// <summary>
    /// 发送聊天请求并返回JSON响应
    /// </summary>
    Task<AIResponse> CompleteChatAsync(
        string systemPrompt,
        string userMessage,
        float temperature,
        int maxTokens,
        string modelName,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// AI响应结果
/// </summary>
public class AIResponse
{
    public string Content { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens { get; set; }
}
