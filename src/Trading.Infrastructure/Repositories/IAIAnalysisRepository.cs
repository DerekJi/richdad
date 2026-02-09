using Trading.Infrastructure.Models;

namespace Trading.Infrastructure.Repositories;

/// <summary>
/// AI分析历史记录仓储接口
/// </summary>
public interface IAIAnalysisRepository
{
    /// <summary>
    /// 保存AI分析记录
    /// </summary>
    Task SaveAnalysisAsync(AIAnalysisHistory analysis);

    /// <summary>
    /// 获取最近的分析记录
    /// </summary>
    Task<List<AIAnalysisHistory>> GetRecentAnalysesAsync(int count = 100);

    /// <summary>
    /// 按品种查询分析记录
    /// </summary>
    Task<List<AIAnalysisHistory>> GetAnalysesBySymbolAsync(string symbol, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 按分析类型查询
    /// </summary>
    Task<List<AIAnalysisHistory>> GetAnalysesByTypeAsync(string analysisType, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 获取分析统计信息
    /// </summary>
    Task<AIAnalysisStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
}

/// <summary>
/// AI分析统计信息
/// </summary>
public class AIAnalysisStatistics
{
    public int TotalAnalyses { get; set; }
    public int SuccessfulAnalyses { get; set; }
    public int FailedAnalyses { get; set; }
    public int CachedAnalyses { get; set; }
    public long TotalEstimatedTokens { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public Dictionary<string, int> AnalysesByType { get; set; } = new();
    public Dictionary<string, int> AnalysesBySymbol { get; set; } = new();
}
