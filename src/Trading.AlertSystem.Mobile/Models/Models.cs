namespace Trading.AlertSystem.Mobile.Models;

/// <summary>
/// 监控状态项
/// </summary>
public class MonitorStatusItem
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal TargetPrice { get; set; }
    public string Direction { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public string TimeFrame { get; set; } = string.Empty;
    public int? EmaPeriod { get; set; }
    public DateTime LastCheckTime { get; set; }

    /// <summary>
    /// 显示用的距离文本
    /// </summary>
    public string DistanceDisplay => $"{Distance:F2}%";

    /// <summary>
    /// 显示用的类型文本
    /// </summary>
    public string TypeDisplay => Type == "PriceMonitor" ? "价格" : "EMA";

    /// <summary>
    /// 价格颜色（正为绿，负为红）
    /// </summary>
    public Color DistanceColor => Distance >= 0 ? Colors.Green : Colors.Red;
}

/// <summary>
/// 价格监控规则
/// </summary>
public class PriceMonitorRule
{
    public string Id { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AlertType Type { get; set; }
    public decimal? TargetPrice { get; set; }
    public PriceDirection Direction { get; set; }
    public int? EmaPeriod { get; set; }
    public int? MaPeriod { get; set; }
    public string TimeFrame { get; set; } = "M5";
    public string MessageTemplate { get; set; } = string.Empty;
    public long? TelegramChatId { get; set; }
    public bool Enabled { get; set; } = true;
    public bool IsTriggered { get; set; }
    public DateTime? LastTriggeredAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 类型显示文本
    /// </summary>
    public string TypeDisplay => Type switch
    {
        AlertType.FixedPrice => "固定价格",
        AlertType.EMA => $"EMA{EmaPeriod}",
        AlertType.MA => $"MA{MaPeriod}",
        _ => "未知"
    };

    /// <summary>
    /// 方向显示文本
    /// </summary>
    public string DirectionDisplay => Direction == PriceDirection.Above ? "上穿" : "下穿";

    /// <summary>
    /// 状态显示文本
    /// </summary>
    public string StatusDisplay => Enabled ? (IsTriggered ? "已触发" : "监控中") : "已禁用";

    /// <summary>
    /// 状态颜色
    /// </summary>
    public Color StatusColor => Enabled ? (IsTriggered ? Colors.Orange : Colors.Green) : Colors.Gray;
}

/// <summary>
/// 告警类型
/// </summary>
public enum AlertType
{
    FixedPrice,
    EMA,
    MA
}

/// <summary>
/// 价格方向
/// </summary>
public enum PriceDirection
{
    Above,
    Below
}

/// <summary>
/// 告警历史
/// </summary>
public class AlertHistory
{
    public string Id { get; set; } = string.Empty;
    public AlertHistoryType Type { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public DateTime AlertTime { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public bool IsSent { get; set; }
    public string? SendTarget { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 类型显示文本
    /// </summary>
    public string TypeDisplay => Type == AlertHistoryType.PriceAlert ? "价格告警" : "EMA穿越";

    /// <summary>
    /// 时间显示
    /// </summary>
    public string TimeDisplay => AlertTime.ToLocalTime().ToString("MM-dd HH:mm:ss");
}

/// <summary>
/// 告警历史类型
/// </summary>
public enum AlertHistoryType
{
    PriceAlert,
    EmaCross
}

/// <summary>
/// 告警历史分页响应
/// </summary>
public class AlertHistoryResponse
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public List<AlertHistory> Items { get; set; } = new();
}

/// <summary>
/// EMA监测配置
/// </summary>
public class EmaMonitoringConfig
{
    public string Id { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public List<string> Symbols { get; set; } = new();
    public List<string> TimeFrames { get; set; } = new();
    public List<int> EmaPeriods { get; set; } = new();
    public int HistoryMultiplier { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;

    /// <summary>
    /// 品种显示
    /// </summary>
    public string SymbolsDisplay => string.Join(", ", Symbols);

    /// <summary>
    /// 周期显示
    /// </summary>
    public string TimeFramesDisplay => string.Join(", ", TimeFrames);

    /// <summary>
    /// EMA显示
    /// </summary>
    public string EmaPeriodsDisplay => string.Join(", ", EmaPeriods.Select(p => $"EMA{p}"));
}

/// <summary>
/// 创建告警规则请求
/// </summary>
public class CreateRuleRequest
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AlertType Type { get; set; }
    public decimal? TargetPrice { get; set; }
    public PriceDirection Direction { get; set; }
    public int? EmaPeriod { get; set; }
    public int? MaPeriod { get; set; }
    public string? TimeFrame { get; set; }
    public string? MessageTemplate { get; set; }
    public long? TelegramChatId { get; set; }
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// 更新告警规则请求
/// </summary>
public class UpdateRuleRequest
{
    public string? Symbol { get; set; }
    public string? Name { get; set; }
    public AlertType? Type { get; set; }
    public decimal? TargetPrice { get; set; }
    public PriceDirection? Direction { get; set; }
    public int? EmaPeriod { get; set; }
    public int? MaPeriod { get; set; }
    public string? TimeFrame { get; set; }
    public string? MessageTemplate { get; set; }
    public long? TelegramChatId { get; set; }
    public bool? Enabled { get; set; }
}
