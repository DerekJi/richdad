namespace Trading.Infrastructure.AI.Models;

/// <summary>
/// 信号验证结果
/// </summary>
public class SignalValidation
{
    /// <summary>
    /// 信号是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 质量分数（0-100）
    /// </summary>
    public int QualityScore { get; set; }

    /// <summary>
    /// 验证原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 风险评估
    /// </summary>
    public RiskLevel Risk { get; set; }

    /// <summary>
    /// 建议操作
    /// </summary>
    public string Recommendation { get; set; } = string.Empty;

    /// <summary>
    /// AI分析详情
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// 验证时间
    /// </summary>
    public DateTime ValidatedAt { get; set; }
}

public enum RiskLevel
{
    Low,
    Medium,
    High
}
