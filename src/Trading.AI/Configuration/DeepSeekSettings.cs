namespace Trading.AI.Configuration;

/// <summary>
/// DeepSeek AI配置
/// </summary>
public class DeepSeekSettings
{
    public const string SectionName = "DeepSeek";

    /// <summary>
    /// DeepSeek API端点
    /// </summary>
    public string Endpoint { get; set; } = "https://api.deepseek.com";

    /// <summary>
    /// API密钥
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 模型名称
    /// </summary>
    public string ModelName { get; set; } = "deepseek-chat";

    /// <summary>
    /// 是否启用DeepSeek
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// 每日最大调用次数限制
    /// </summary>
    public int MaxDailyRequests { get; set; } = 500;

    /// <summary>
    /// 每月预算限制（美元）
    /// </summary>
    public decimal MonthlyBudgetLimit { get; set; } = 20m;

    /// <summary>
    /// 请求超时时间（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// 温度参数（0-2）
    /// </summary>
    public float Temperature { get; set; } = 0.3f;

    /// <summary>
    /// 最大输出Token数
    /// </summary>
    public int MaxTokens { get; set; } = 2000;

    /// <summary>
    /// 输入Token成本（每百万tokens，美元）
    /// </summary>
    public decimal CostPer1MInputTokens { get; set; } = 0.14m;

    /// <summary>
    /// 输出Token成本（每百万tokens，美元）
    /// </summary>
    public decimal CostPer1MOutputTokens { get; set; } = 0.28m;
}
