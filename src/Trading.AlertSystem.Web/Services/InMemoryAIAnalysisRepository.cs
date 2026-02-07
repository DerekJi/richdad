using Trading.AI.Models;
using Trading.AlertSystem.Data.Models;
using Trading.AlertSystem.Data.Repositories;

namespace Trading.AlertSystem.Web.Services;

/// <summary>
/// 内存版本的AI分析历史仓储（无需数据库）
/// </summary>
public class InMemoryAIAnalysisRepository : IAIAnalysisRepository
{
    private readonly List<AIAnalysisHistory> _history = new();
    private readonly ILogger<InMemoryAIAnalysisRepository> _logger;
    private readonly object _lock = new();

    public InMemoryAIAnalysisRepository(ILogger<InMemoryAIAnalysisRepository> logger)
    {
        _logger = logger;
    }

    public Task SaveAnalysisAsync(AIAnalysisHistory analysis)
    {
        lock (_lock)
        {
            if (string.IsNullOrEmpty(analysis.Id))
                analysis.Id = Guid.NewGuid().ToString();
            if (analysis.AnalysisTime == default)
                analysis.AnalysisTime = DateTime.UtcNow;
            _history.Add(analysis);

            // 只保留最近500条记录
            if (_history.Count > 500)
            {
                _history.RemoveAt(0);
            }

            return Task.CompletedTask;
        }
    }

    public Task<List<AIAnalysisHistory>> GetRecentAnalysesAsync(int count = 100)
    {
        lock (_lock)
        {
            var recent = _history
                .OrderByDescending(h => h.AnalysisTime)
                .Take(count)
                .ToList();
            return Task.FromResult(recent);
        }
    }

    public Task<List<AIAnalysisHistory>> GetAnalysesBySymbolAsync(string symbol, DateTime? startDate = null, DateTime? endDate = null)
    {
        lock (_lock)
        {
            var query = _history.Where(h => h.Symbol == symbol);

            if (startDate.HasValue)
                query = query.Where(h => h.AnalysisTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(h => h.AnalysisTime <= endDate.Value);

            return Task.FromResult(query.ToList());
        }
    }

    public Task<List<AIAnalysisHistory>> GetAnalysesByTypeAsync(string analysisType, DateTime? startDate = null, DateTime? endDate = null)
    {
        lock (_lock)
        {
            var query = _history.Where(h => h.AnalysisType == analysisType);

            if (startDate.HasValue)
                query = query.Where(h => h.AnalysisTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(h => h.AnalysisTime <= endDate.Value);

            return Task.FromResult(query.ToList());
        }
    }

    public Task<AIAnalysisStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        lock (_lock)
        {
            var query = _history.AsEnumerable();

            if (startDate.HasValue)
                query = query.Where(h => h.AnalysisTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(h => h.AnalysisTime <= endDate.Value);

            var stats = new AIAnalysisStatistics
            {
                TotalAnalyses = query.Count(),
                SuccessfulAnalyses = query.Count(h => h.IsSuccess),
                FailedAnalyses = query.Count(h => !h.IsSuccess),
                CachedAnalyses = query.Count(h => h.FromCache),
                TotalEstimatedTokens = query.Sum(h => h.EstimatedTokens),
                AverageResponseTimeMs = query.Any() ? query.Average(h => h.ResponseTimeMs) : 0,
                AnalysesByType = query.GroupBy(h => h.AnalysisType).ToDictionary(g => g.Key, g => g.Count()),
                AnalysesBySymbol = query.GroupBy(h => h.Symbol).ToDictionary(g => g.Key, g => g.Count())
            };

            return Task.FromResult(stats);
        }
    }
}
