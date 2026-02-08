using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Trading.Infras.Data.Models;

/// <summary>
/// 告警触发记录（已触发的告警日志）
/// 记录每次告警触发的详细信息，包括价格告警和EMA穿越告警
/// </summary>
public class AlertHistory
{
    /// <summary>
    /// 唯一标识符
    /// </summary>
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 告警类型
    /// </summary>
    public AlertHistoryType Type { get; set; }

    /// <summary>
    /// 品种
    /// </summary>
    [JsonPropertyName("symbol")]
    [JsonProperty("symbol")]
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// 告警时间
    /// </summary>
    public DateTime AlertTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 告警消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 告警详细数据（JSON格式）
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// 是否已发送
    /// </summary>
    public bool IsSent { get; set; }

    /// <summary>
    /// 发送目标（如 Telegram ChatId）
    /// </summary>
    public string? SendTarget { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 告警历史类型
/// </summary>
public enum AlertHistoryType
{
    /// <summary>
    /// 价格触发告警
    /// </summary>
    PriceAlert,

    /// <summary>
    /// EMA穿越告警
    /// </summary>
    EmaCross
}

/// <summary>
/// 价格告警详细信息
/// </summary>
public class PriceAlertDetails
{
    public decimal TargetPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public string Direction { get; set; } = string.Empty; // "Above" or "Below"
}

/// <summary>
/// EMA穿越告警详细信息
/// </summary>
public class EmaCrossAlertDetails
{
    public string TimeFrame { get; set; } = string.Empty;
    public int EmaPeriod { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal EmaValue { get; set; }
    public string CrossType { get; set; } = string.Empty; // "CrossAbove" or "CrossBelow"
}
