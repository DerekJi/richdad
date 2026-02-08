namespace Trading.AI.Services;

/// <summary>
/// DeepSeek AI服务接口
/// </summary>
public interface IDeepSeekService
{
    /// <summary>
    /// 发送聊天请求
    /// </summary>
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
