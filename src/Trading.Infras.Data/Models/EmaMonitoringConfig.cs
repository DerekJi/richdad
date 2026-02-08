using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Trading.Infras.Data.Models;

/// <summary>
/// EMA监测配置（持久化到数据库）
/// </summary>
public class EmaMonitoringConfig
{
    /// <summary>
    /// 配置ID（固定为"default"，单例配置）
    /// </summary>
    [JsonPropertyName("id")]  // For System.Text.Json
    [JsonProperty("id")]      // For Newtonsoft.Json (CosmosDB)
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用EMA监测
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 监测的交易品种列表
    /// </summary>
    public List<string> Symbols { get; set; } = new();

    /// <summary>
    /// 监测的K线周期列表（M1, M5, M15, M30, H1, H4, D1）
    /// </summary>
    public List<string> TimeFrames { get; set; } = new();

    /// <summary>
    /// EMA周期列表（例如：20, 60, 120）
    /// </summary>
    public List<int> EmaPeriods { get; set; } = new();

    /// <summary>
    /// 历史数据倍数（用于计算需要获取多少根K线）
    /// </summary>
    public int HistoryMultiplier { get; set; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 最后更新者
    /// </summary>
    public string UpdatedBy { get; set; } = string.Empty;
}
