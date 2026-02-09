using Trading.Infrastructure.AI.Models;

namespace Trading.Services.Services;

/// <summary>
/// 双级AI监控服务接口
/// </summary>
public interface IDualTierMonitoringService
{
    /// <summary>
    /// 分析市场数据并决定是否发送交易信号
    /// </summary>
    /// <param name="symbol">交易品种</param>
    /// <param name="timeFrame">时间周期</param>
    /// <param name="marketData">市场数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分析结果，如果应该发送信号则包含完整的分析</returns>
    Task<DualTierAnalysisResult?> AnalyzeAndFilterAsync(
        string symbol,
        string timeFrame,
        string marketData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取今日使用统计
    /// </summary>
    (int tier1Calls, int tier2Calls, int filtered, decimal cost) GetTodayStats();
}
