using System.Text.Json.Serialization;
using Trading.Backtest.Data.Infrastructure;
using Trading.Models;

namespace Trading.Backtest.Data.Models;

/// <summary>
/// 回测结果
/// </summary>
public class BacktestResult
{
    /// <summary>
    /// 结果ID (Cosmos DB要求小写)
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 回测时间
    /// </summary>
    public DateTime BacktestTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 策略配置
    /// </summary>
    public StrategyConfig Config { get; set; } = new();

    /// <summary>
    /// 所有交易记录
    /// </summary>
    public List<Trade> Trades { get; set; } = new();

    /// <summary>
    /// 回测开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 回测结束时间
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 总体统计
    /// </summary>
    public PerformanceMetrics OverallMetrics { get; set; } = new();

    /// <summary>
    /// 每周统计
    /// </summary>
    public List<PeriodMetrics> WeeklyMetrics { get; set; } = new();

    /// <summary>
    /// 每月统计
    /// </summary>
    public List<PeriodMetrics> MonthlyMetrics { get; set; } = new();

    /// <summary>
    /// 每年统计
    /// </summary>
    public List<PeriodMetrics> YearlyMetrics { get; set; } = new();

    /// <summary>
    /// 收益曲线数据点
    /// </summary>
    public List<EquityPoint> EquityCurve { get; set; } = new();
}

/// <summary>
/// 总体性能指标
/// </summary>
public class PerformanceMetrics
{
    /// <summary>
    /// 总交易数
    /// </summary>
    public int TotalTrades { get; set; }

    /// <summary>
    /// 盈利交易数
    /// </summary>
    public int WinningTrades { get; set; }

    /// <summary>
    /// 亏损交易数
    /// </summary>
    public int LosingTrades { get; set; }

    /// <summary>
    /// 胜率 (%)
    /// </summary>
    [JsonIgnore]
    public decimal WinRate => TotalTrades > 0 ? (decimal)WinningTrades / TotalTrades * 100 : 0;

    /// <summary>
    /// 总收益 (点数)
    /// </summary>
    public decimal TotalProfit { get; set; }

    /// <summary>
    /// 总收益率 (相对于风险)
    /// </summary>
    public decimal TotalReturnRate { get; set; }

    /// <summary>
    /// 平均持仓时间
    /// </summary>
    [JsonConverter(typeof(TimeSpanConverter))]
    public TimeSpan AverageHoldingTime { get; set; }

    /// <summary>
    /// 最大连续盈利单数
    /// </summary>
    public int MaxConsecutiveWins { get; set; }

    /// <summary>
    /// 最大连续盈利开始时间
    /// </summary>
    public DateTime? MaxConsecutiveWinsStartTime { get; set; }

    /// <summary>
    /// 最大连续盈利结束时间
    /// </summary>
    public DateTime? MaxConsecutiveWinsEndTime { get; set; }

    /// <summary>
    /// 最大连续亏损单数
    /// </summary>
    public int MaxConsecutiveLosses { get; set; }

    /// <summary>
    /// 最大连续亏损开始时间
    /// </summary>
    public DateTime? MaxConsecutiveLossesStartTime { get; set; }

    /// <summary>
    /// 最大连续亏损结束时间
    /// </summary>
    public DateTime? MaxConsecutiveLossesEndTime { get; set; }

    /// <summary>
    /// 最大回撤
    /// </summary>
    public decimal MaxDrawdown { get; set; }

    /// <summary>
    /// 最大回撤开始时间
    /// </summary>
    public DateTime? MaxDrawdownStartTime { get; set; }

    /// <summary>
    /// 最大回撤结束时间
    /// </summary>
    public DateTime? MaxDrawdownEndTime { get; set; }

    /// <summary>
    /// 平均每月开仓单数
    /// </summary>
    public decimal AverageTradesPerMonth { get; set; }

    /// <summary>
    /// 盈亏比
    /// </summary>
    public decimal ProfitFactor { get; set; }
}

/// <summary>
/// 周期统计指标
/// </summary>
public class PeriodMetrics
{
    /// <summary>
    /// 周期标识 (如 "2024-W01", "2024-01", "2024")
    /// </summary>
    public string Period { get; set; } = string.Empty;

    /// <summary>
    /// 周期开始时间
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 周期结束时间
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// 交易次数
    /// </summary>
    public int TradeCount { get; set; }

    /// <summary>
    /// 盈利交易数
    /// </summary>
    public int WinningTrades { get; set; }

    /// <summary>
    /// 胜率 (%)
    /// </summary>
    [JsonIgnore]
    public decimal WinRate => TradeCount > 0 ? (decimal)WinningTrades / TradeCount * 100 : 0;

    /// <summary>
    /// 收益额/盈亏额 (点数)
    /// </summary>
    public decimal ProfitLoss { get; set; }

    /// <summary>
    /// 收益率
    /// </summary>
    public decimal ReturnRate { get; set; }

    /// <summary>
    /// 平均持仓时间
    /// </summary>
    [JsonConverter(typeof(TimeSpanConverter))]
    public TimeSpan AverageHoldingTime { get; set; }

    /// <summary>
    /// 最大连续盈利单数
    /// </summary>
    public int MaxConsecutiveWins { get; set; }

    /// <summary>
    /// 最大连续亏损单数
    /// </summary>
    public int MaxConsecutiveLosses { get; set; }
}

/// <summary>
/// 收益曲线数据点
/// </summary>
public class EquityPoint
{
    /// <summary>
    /// 时间
    /// </summary>
    public DateTime Time { get; set; }

    /// <summary>
    /// 累计收益 (点数)
    /// </summary>
    public decimal CumulativeProfit { get; set; }

    /// <summary>
    /// 累计收益率
    /// </summary>
    public decimal CumulativeReturnRate { get; set; }

    /// <summary>
    /// 交易ID
    /// </summary>
    public string? TradeId { get; set; }
}
