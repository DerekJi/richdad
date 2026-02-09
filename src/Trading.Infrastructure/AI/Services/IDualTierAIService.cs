using Trading.Infrastructure.AI.Models;

namespace Trading.Infrastructure.AI.Services;

/// <summary>
/// 双级AI分析服务接口
/// </summary>
public interface IDualTierAIService
{
    /// <summary>
    /// 执行双级AI分析
    /// </summary>
    /// <param name="marketData">市场数据（CSV格式或压缩格式）</param>
    /// <param name="symbol">交易品种</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>双级分析结果</returns>
    Task<DualTierAnalysisResult> AnalyzeAsync(
        string marketData,
        string symbol,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 仅执行Tier1过滤
    /// </summary>
    Task<Tier1FilterResult> ExecuteTier1FilterAsync(
        string marketData,
        string symbol,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行Tier2深度分析（通常在Tier1通过后调用）
    /// </summary>
    Task<Tier2AnalysisResult> ExecuteTier2AnalysisAsync(
        string marketData,
        string symbol,
        Tier1FilterResult? tier1Result = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查是否已达到每日调用限制
    /// </summary>
    bool IsRateLimitReached();

    /// <summary>
    /// 获取今日已使用的调用次数
    /// </summary>
    int GetTodayUsageCount();

    /// <summary>
    /// 获取本月预估成本（美元）
    /// </summary>
    decimal GetEstimatedMonthlyCost();
}
