namespace Trading.Infrastructure.AI.Services;

/// <summary>
/// Azure OpenAI服务接口
/// </summary>
public interface IAzureOpenAIService
{
    /// <summary>
    /// 发送聊天请求
    /// </summary>
    /// <param name="systemPrompt">系统提示词</param>
    /// <param name="userMessage">用户消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>AI响应</returns>
    Task<string> ChatCompletionAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查是否已达到每日调用限制
    /// </summary>
    bool IsRateLimitReached();

    /// <summary>
    /// 获取今日已使用的调用次数
    /// </summary>
    int GetTodayUsageCount();

    /// <summary>
    /// 获取本月预估成本（美元）
    /// </summary>
    decimal GetEstimatedMonthlyCost();
}
