using Trading.AI.Models;
using Trading.Data.Models;

namespace Trading.AI.Services;

/// <summary>
/// 市场分析服务接口
/// </summary>
public interface IMarketAnalysisService
{
    /// <summary>
    /// 分析多时间框架趋势
    /// </summary>
    /// <param name="symbol">交易品种</param>
    /// <param name="timeFrames">时间框架列表（如 H1, H4, D1）</param>
    /// <param name="candlesByTimeFrame">每个时间框架的K线数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>每个时间框架的趋势分析</returns>
    Task<Dictionary<string, TrendAnalysis>> AnalyzeMultiTimeFrameTrendAsync(
        string symbol,
        List<string> timeFrames,
        Dictionary<string, List<Candle>> candlesByTimeFrame,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 识别关键支撑和阻力位
    /// </summary>
    /// <param name="symbol">交易品种</param>
    /// <param name="candles">K线数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关键价格位分析</returns>
    Task<KeyLevelsAnalysis> IdentifyKeyLevelsAsync(
        string symbol,
        List<Candle> candles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证Pin Bar信号质量
    /// </summary>
    /// <param name="symbol">交易品种</param>
    /// <param name="pinBar">Pin Bar K线</param>
    /// <param name="direction">交易方向</param>
    /// <param name="trendAnalyses">多时间框架趋势分析（可选，用于缓存）</param>
    /// <param name="keyLevels">关键价格位（可选，用于缓存）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>信号验证结果</returns>
    Task<SignalValidation> ValidatePinBarSignalAsync(
        string symbol,
        Candle pinBar,
        TradeDirection direction,
        Dictionary<string, TrendAnalysis>? trendAnalyses = null,
        KeyLevelsAnalysis? keyLevels = null,
        CancellationToken cancellationToken = default);
}
