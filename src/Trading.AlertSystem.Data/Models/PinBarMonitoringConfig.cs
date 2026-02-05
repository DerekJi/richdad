using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Trading.AlertSystem.Data.Models;

/// <summary>
/// PinBar监控配置（持久化到数据库）
/// </summary>
public class PinBarMonitoringConfig
{
    /// <summary>
    /// 配置ID（固定为"default"，单例配置）
    /// </summary>
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = "default";

    /// <summary>
    /// 是否启用PinBar监控
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
    /// 策略配置
    /// </summary>
    public PinBarStrategySettings StrategySettings { get; set; } = new();

    /// <summary>
    /// 历史数据倍数（用于计算需要获取多少根K线，基于最大EMA周期）
    /// </summary>
    public int HistoryMultiplier { get; set; } = 3;

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 最后更新者
    /// </summary>
    public string UpdatedBy { get; set; } = string.Empty;
}

/// <summary>
/// PinBar策略设置
/// </summary>
public class PinBarStrategySettings
{
    /// <summary>
    /// 策略名称
    /// </summary>
    public string StrategyName { get; set; } = "PinBar";

    /// <summary>
    /// 基准EMA周期（通常是200）
    /// </summary>
    public int BaseEma { get; set; } = 200;

    /// <summary>
    /// 要检查的EMA列表（用于判断是否靠近EMA）
    /// </summary>
    public List<int> EmaList { get; set; } = new();

    /// <summary>
    /// 靠近EMA的阈值（价格距离）
    /// </summary>
    public decimal NearEmaThreshold { get; set; } = 0.001m;

    /// <summary>
    /// 最小波动阈值
    /// </summary>
    public decimal Threshold { get; set; } = 0.0001m;

    /// <summary>
    /// 最小下影线/ATR比率
    /// </summary>
    public decimal MinLowerWickAtrRatio { get; set; } = 1.2m;

    /// <summary>
    /// 最大实体百分比
    /// </summary>
    public decimal MaxBodyPercentage { get; set; } = 35m;

    /// <summary>
    /// 最小长影线百分比
    /// </summary>
    public decimal MinLongerWickPercentage { get; set; } = 50m;

    /// <summary>
    /// 最大短影线百分比
    /// </summary>
    public decimal MaxShorterWickPercentage { get; set; } = 25m;

    /// <summary>
    /// 是否要求PinBar方向匹配（看涨PinBar必须是阳线）
    /// </summary>
    public bool RequirePinBarDirectionMatch { get; set; } = true;

    /// <summary>
    /// 是否要求EMA对齐（价格需要在EMA附近）
    /// </summary>
    public bool RequireEmaAlignment { get; set; } = true;

    /// <summary>
    /// 最小ADX值（0表示不限制）
    /// </summary>
    public decimal MinAdx { get; set; } = 0m;

    /// <summary>
    /// 低ADX时的盈亏比（0表示不在低ADX时开仓）
    /// </summary>
    public decimal LowAdxRiskRewardRatio { get; set; } = 0m;

    /// <summary>
    /// 标准盈亏比
    /// </summary>
    public decimal RiskRewardRatio { get; set; } = 2m;

    /// <summary>
    /// 是否限制交易时间
    /// </summary>
    public bool NoTradingHoursLimit { get; set; } = true;

    /// <summary>
    /// 开始交易时间（UTC小时）
    /// </summary>
    public int StartTradingHour { get; set; } = 0;

    /// <summary>
    /// 结束交易时间（UTC小时）
    /// </summary>
    public int EndTradingHour { get; set; } = 23;

    /// <summary>
    /// 禁止交易的小时列表（UTC）
    /// </summary>
    public List<int>? NoTradeHours { get; set; }

    /// <summary>
    /// 止损策略
    /// </summary>
    public string StopLossStrategy { get; set; } = "PinbarEndPlusAtr";

    /// <summary>
    /// 止损ATR倍数
    /// </summary>
    public decimal StopLossAtrRatio { get; set; } = 0.3m;

    /// <summary>
    /// ATR周期
    /// </summary>
    public int AtrPeriod { get; set; } = 14;

    /// <summary>
    /// 是否要求成交量确认
    /// </summary>
    public bool RequireVolumeConfirm { get; set; } = false;

    /// <summary>
    /// 最小成交量倍数
    /// </summary>
    public decimal MinVolumeMultiplier { get; set; } = 1.2m;

    /// <summary>
    /// 成交量回看周期
    /// </summary>
    public int VolumeLookbackPeriod { get; set; } = 10;
}

/// <summary>
/// PinBar信号历史记录
/// </summary>
public class PinBarSignalHistory
{
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 品种
    /// </summary>
    [JsonPropertyName("symbol")]
    [JsonProperty("symbol")]
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// 时间周期
    /// </summary>
    public string TimeFrame { get; set; } = string.Empty;

    /// <summary>
    /// 信号时间
    /// </summary>
    public DateTime SignalTime { get; set; }

    /// <summary>
    /// 交易方向
    /// </summary>
    public string Direction { get; set; } = string.Empty; // "Long" or "Short"

    /// <summary>
    /// PinBar K线时间
    /// </summary>
    public DateTime PinBarTime { get; set; }

    /// <summary>
    /// 入场价
    /// </summary>
    public decimal EntryPrice { get; set; }

    /// <summary>
    /// 止损价
    /// </summary>
    public decimal StopLoss { get; set; }

    /// <summary>
    /// 止盈价
    /// </summary>
    public decimal TakeProfit { get; set; }

    /// <summary>
    /// 盈亏比
    /// </summary>
    public decimal RiskRewardRatio { get; set; }

    /// <summary>
    /// ADX值
    /// </summary>
    public decimal Adx { get; set; }

    /// <summary>
    /// 是否已发送Telegram
    /// </summary>
    public bool IsSent { get; set; }

    /// <summary>
    /// 消息内容
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
