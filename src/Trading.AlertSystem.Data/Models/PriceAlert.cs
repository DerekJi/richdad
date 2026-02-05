using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Trading.AlertSystem.Data.Models;

/// <summary>
/// 告警规则配置（待触发的告警条件）
/// 当满足条件时触发告警，并生成AlertHistory记录
/// </summary>
public class PriceAlert
{
    /// <summary>
    /// 告警ID
    /// </summary>
    [JsonPropertyName("id")]  // For System.Text.Json
    [JsonProperty("id")]      // For Newtonsoft.Json (CosmosDB)
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 交易品种
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// 告警名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 告警类型
    /// </summary>
    public AlertType Type { get; set; }

    /// <summary>
    /// 目标价格（当Type为FixedPrice时使用）
    /// </summary>
    public decimal? TargetPrice { get; set; }

    /// <summary>
    /// 价格方向（上穿或下穿）
    /// </summary>
    public PriceDirection Direction { get; set; }

    /// <summary>
    /// EMA周期（当Type为EMA时使用）
    /// </summary>
    public int? EmaPeriod { get; set; }

    /// <summary>
    /// MA周期（当Type为MA时使用）
    /// </summary>
    public int? MaPeriod { get; set; }

    /// <summary>
    /// 时间周期（用于计算指标）
    /// </summary>
    public string TimeFrame { get; set; } = "M5";

    /// <summary>
    /// 消息模板
    /// </summary>
    public string MessageTemplate { get; set; } = "【{Symbol}】价格告警：当前价格 {Price} 已{Direction} {Target}";

    /// <summary>
    /// Telegram Chat ID（如果为空则使用默认配置）
    /// </summary>
    public long? TelegramChatId { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 是否已触发（避免重复发送）
    /// </summary>
    public bool IsTriggered { get; set; }

    /// <summary>
    /// 最后触发时间
    /// </summary>
    public DateTime? LastTriggeredAt { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 告警类型
/// </summary>
public enum AlertType
{
    /// <summary>
    /// 固定价格
    /// </summary>
    FixedPrice,

    /// <summary>
    /// 指数移动平均线（EMA）
    /// </summary>
    EMA,

    /// <summary>
    /// 简单移动平均线（MA/SMA）
    /// </summary>
    MA
}

/// <summary>
/// 价格方向
/// </summary>
public enum PriceDirection
{
    /// <summary>
    /// 上穿（价格从下方突破到上方）
    /// </summary>
    Above,

    /// <summary>
    /// 下穿（价格从上方跌破到下方）
    /// </summary>
    Below
}
