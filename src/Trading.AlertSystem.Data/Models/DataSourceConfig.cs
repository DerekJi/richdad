using System.Text.Json.Serialization;

namespace Trading.AlertSystem.Data.Models;

/// <summary>
/// 数据源配置
/// </summary>
public class DataSourceConfig
{
    /// <summary>
    /// 配置ID（固定值 "current"）
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = "current";

    /// <summary>
    /// 数据提供商：TradeLocker 或 Oanda
    /// </summary>
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = "Oanda";

    /// <summary>
    /// 最后更新时间
    /// </summary>
    [JsonPropertyName("lastUpdated")]
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// 更新备注
    /// </summary>
    [JsonPropertyName("updatedBy")]
    public string? UpdatedBy { get; set; }
}
