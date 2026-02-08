using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Trading.Infras.Data.Models;

/// <summary>
/// AI分析历史记录
/// </summary>
public class AIAnalysisHistory
{
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 分析类型（TrendAnalysis, KeyLevels, SignalValidation）
    /// </summary>
    [JsonPropertyName("analysisType")]
    [JsonProperty("analysisType")]
    public string AnalysisType { get; set; } = string.Empty;

    /// <summary>
    /// 品种
    /// </summary>
    [JsonPropertyName("symbol")]
    [JsonProperty("symbol")]
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// 时间周期（可选）
    /// </summary>
    [JsonPropertyName("timeFrame")]
    [JsonProperty("timeFrame")]
    public string? TimeFrame { get; set; }

    /// <summary>
    /// 分析时间
    /// </summary>
    [JsonPropertyName("analysisTime")]
    [JsonProperty("analysisTime")]
    public DateTime AnalysisTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 输入数据（JSON格式）
    /// </summary>
    [JsonPropertyName("inputData")]
    [JsonProperty("inputData")]
    public string InputData { get; set; } = string.Empty;

    /// <summary>
    /// AI原始响应
    /// </summary>
    [JsonPropertyName("rawResponse")]
    [JsonProperty("rawResponse")]
    public string RawResponse { get; set; } = string.Empty;

    /// <summary>
    /// 解析后的结果（JSON格式）
    /// </summary>
    [JsonPropertyName("parsedResult")]
    [JsonProperty("parsedResult")]
    public string ParsedResult { get; set; } = string.Empty;

    /// <summary>
    /// 是否成功
    /// </summary>
    [JsonPropertyName("isSuccess")]
    [JsonProperty("isSuccess")]
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息（如果失败）
    /// </summary>
    [JsonPropertyName("errorMessage")]
    [JsonProperty("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 使用的Token数量（估算）
    /// </summary>
    [JsonPropertyName("estimatedTokens")]
    [JsonProperty("estimatedTokens")]
    public int EstimatedTokens { get; set; }

    /// <summary>
    /// 响应时间（毫秒）
    /// </summary>
    [JsonPropertyName("responseTimeMs")]
    [JsonProperty("responseTimeMs")]
    public long ResponseTimeMs { get; set; }

    /// <summary>
    /// 是否使用缓存
    /// </summary>
    [JsonPropertyName("fromCache")]
    [JsonProperty("fromCache")]
    public bool FromCache { get; set; }

    /// <summary>
    /// 关联的信号ID（如果是信号验证）
    /// </summary>
    [JsonPropertyName("relatedSignalId")]
    [JsonProperty("relatedSignalId")]
    public string? RelatedSignalId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonPropertyName("createdAt")]
    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
