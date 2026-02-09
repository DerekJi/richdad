namespace Trading.Infrastructure.AI.Configuration;

/// <summary>
/// AI提供商类型
/// </summary>
public enum AIProvider
{
    AzureOpenAI,
    DeepSeek
}

/// <summary>
/// 双级AI模型配置
/// </summary>
public class DualTierAISettings
{
    public const string SectionName = "DualTierAI";

    /// <summary>
    /// 是否启用双级AI架构
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// AI提供商（AzureOpenAI, DeepSeek）
    /// </summary>
    public string Provider { get; set; } = "AzureOpenAI";

    /// <summary>
    /// Tier1最小通过分数（0-100）
    /// </summary>
    public int Tier1MinScore { get; set; } = 70;

    /// <summary>
    /// Tier1模型配置（GPT-4o-mini）
    /// </summary>
    public TierModelSettings Tier1 { get; set; } = new()
    {
        DeploymentName = "gpt-4o-mini",
        Temperature = 0.3f,
        MaxTokens = 500,
        CostPer1MInputTokens = 0.15m,  // $0.15 per 1M input tokens
        CostPer1MOutputTokens = 0.60m  // $0.60 per 1M output tokens
    };

    /// <summary>
    /// Tier2模型配置（GPT-4o）
    /// </summary>
    public TierModelSettings Tier2 { get; set; } = new()
    {
        DeploymentName = "gpt-4o",
        Temperature = 0.5f,
        MaxTokens = 2000,
        CostPer1MInputTokens = 2.50m,  // $2.50 per 1M input tokens
        CostPer1MOutputTokens = 10.00m // $10.00 per 1M output tokens
    };

    /// <summary>
    /// 是否在Tier2请求中包含Tier1总结
    /// </summary>
    public bool IncludeTier1SummaryInTier2 { get; set; } = true;

    /// <summary>
    /// 每日最大调用次数（所有Tier总和）
    /// </summary>
    public int MaxDailyRequests { get; set; } = 500;

    /// <summary>
    /// 每月成本预算（美元）
    /// </summary>
    public decimal MonthlyBudgetLimit { get; set; } = 50m;
}

/// <summary>
/// 单个Tier的模型配置
/// </summary>
public class TierModelSettings
{
    /// <summary>
    /// 部署名称
    /// </summary>
    public string DeploymentName { get; set; } = string.Empty;

    /// <summary>
    /// 温度参数
    /// </summary>
    public float Temperature { get; set; } = 0.3f;

    /// <summary>
    /// 最大Token数
    /// </summary>
    public int MaxTokens { get; set; } = 500;

    /// <summary>
    /// 输入Token成本（每100万）
    /// </summary>
    public decimal CostPer1MInputTokens { get; set; }

    /// <summary>
    /// 输出Token成本（每100万）
    /// </summary>
    public decimal CostPer1MOutputTokens { get; set; }
}
