namespace Trading.Infrastructure.AI.Configuration;

/// <summary>
/// Azure OpenAI配置
/// </summary>
public class AzureOpenAISettings
{
    public const string SectionName = "AzureOpenAI";

    /// <summary>
    /// Azure OpenAI端点URL
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// API密钥
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 部署名称（模型）
    /// </summary>
    public string DeploymentName { get; set; } = "gpt-4o";

    /// <summary>
    /// 是否启用AI功能
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// 每日最大调用次数限制（成本控制）
    /// </summary>
    public int MaxDailyRequests { get; set; } = 500;

    /// <summary>
    /// 每月预算限制（美元）
    /// </summary>
    public decimal MonthlyBudgetLimit { get; set; } = 50m;

    /// <summary>
    /// 请求超时时间（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// 温度参数（0-2，越低越确定性）
    /// </summary>
    public float Temperature { get; set; } = 0.3f;

    /// <summary>
    /// 最大输出Token数
    /// </summary>
    public int MaxTokens { get; set; } = 2000;
}
