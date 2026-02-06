namespace Trading.AI.Models;

/// <summary>
/// 关键价格位
/// </summary>
public class KeyPriceLevel
{
    /// <summary>
    /// 价格位
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// 类型：Support, Resistance
    /// </summary>
    public LevelType Type { get; set; }

    /// <summary>
    /// 强度（0-100）
    /// </summary>
    public int Strength { get; set; }

    /// <summary>
    /// 触碰次数
    /// </summary>
    public int TouchCount { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

public enum LevelType
{
    Support,
    Resistance
}

/// <summary>
/// 关键价格位分析结果
/// </summary>
public class KeyLevelsAnalysis
{
    /// <summary>
    /// 交易品种
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// 支撑位列表
    /// </summary>
    public List<KeyPriceLevel> SupportLevels { get; set; } = new();

    /// <summary>
    /// 阻力位列表
    /// </summary>
    public List<KeyPriceLevel> ResistanceLevels { get; set; } = new();

    /// <summary>
    /// 分析时间
    /// </summary>
    public DateTime AnalyzedAt { get; set; }

    /// <summary>
    /// AI分析详情
    /// </summary>
    public string Details { get; set; } = string.Empty;
}
