namespace Trading.Models;

/// <summary>
/// 为 AI Prompt 准备的处理后市场数据
/// </summary>
/// <remarks>
/// 整合 K 线数据、技术指标、形态识别结果，
/// 以 Markdown 表格形式提供给 AI 进行 Al Brooks 价格行为分析
/// </remarks>
public class ProcessedMarketData
{
    /// <summary>
    /// 品种代码
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// 时间周期
    /// </summary>
    public string TimeFrame { get; set; } = string.Empty;

    /// <summary>
    /// 上下文表格（完整历史数据，用于 AI 理解市场结构）
    /// 格式：Markdown 表格，包含所有可用 K 线
    /// </summary>
    public string ContextTable { get; set; } = string.Empty;

    /// <summary>
    /// 聚焦表格（最近 30 根 K 线，重点分析区域）
    /// 格式：Markdown 表格，用于 AI 详细分析最新走势
    /// </summary>
    public string FocusTable { get; set; } = string.Empty;

    /// <summary>
    /// 形态摘要（文本描述）
    /// 例如："检测到 ii 结构在 Bar -5, -3"
    /// </summary>
    public string PatternSummary { get; set; } = string.Empty;

    /// <summary>
    /// 原始 K 线数据（供非 Prompt 用途使用）
    /// </summary>
    public List<Candle> RawCandles { get; set; } = new();

    /// <summary>
    /// EMA20 值数组（与 RawCandles 对应）
    /// </summary>
    public decimal[] EMA20Values { get; set; } = Array.Empty<decimal>();

    /// <summary>
    /// 形态标签列表（按 K 线索引）
    /// Key: K 线索引，Value: 形态标签列表
    /// </summary>
    public Dictionary<int, List<string>> PatternsByIndex { get; set; } = new();

    /// <summary>
    /// 当前价格（最新 K 线收盘价）
    /// </summary>
    public decimal CurrentPrice => RawCandles.LastOrDefault()?.Close ?? 0;

    /// <summary>
    /// 当前 EMA20
    /// </summary>
    public decimal CurrentEMA20 => EMA20Values.Length > 0 ? EMA20Values[^1] : 0;

    /// <summary>
    /// 数据时间范围
    /// </summary>
    public DateTime StartTime => RawCandles.FirstOrDefault()?.DateTime ?? DateTime.MinValue;
    public DateTime EndTime => RawCandles.LastOrDefault()?.DateTime ?? DateTime.MinValue;

    /// <summary>
    /// K 线数量
    /// </summary>
    public int CandleCount => RawCandles.Count;
}
