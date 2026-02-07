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
            analysis.Id = Guid.NewGuid().ToString();
            analysis.Timestamp = DateTime.UtcNow;
            _history.Add(analysis);

            // 只保留最近500条记录
            if (_history.Count > 500)
            {
                _history.RemoveAt(0);
            }

            return Task.CompletedTask;
        }
    }

    public Task<IEnumerable<AIAnalysisHistory>> GetRecentAsync(int count = 100)
    {
        lock (_lock)
        {
            var recent = _history
                .OrderByDescending(h => h.Timestamp)
                .Take(count)
                .ToList();
            return Task.FromResult<IEnumerable<AIAnalysisHistory>>(recent);
        }
    }

    public Task<IEnumerable<AIAnalysisHistory>> GetBySymbolAsync(string symbol, DateTime? startDate = null, DateTime? endDate = null)
    {
        lock (_lock)
        {
            var query = _history.Where(h => h.Symbol == symbol);

            if (startDate.HasValue)
                query = query.Where(h => h.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(h => h.Timestamp <= endDate.Value);

            return Task.FromResult<IEnumerable<AIAnalysisHistory>>(query.ToList());
        }
    }

    public Task<decimal> GetTotalCostAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        lock (_lock)
        {
            var query = _history.AsEnumerable();

            if (startDate.HasValue)
                query = query.Where(h => h.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(h => h.Timestamp <= endDate.Value);

            return Task.FromResult(query.Sum(h => h.CostUsd));
        }
    }

    public Task<int> GetAnalysisCountAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        lock (_lock)
        {
            var query = _history.AsEnumerable();

            if (startDate.HasValue)
                query = query.Where(h => h.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(h => h.Timestamp <= endDate.Value);

            return Task.FromResult(query.Count());
        }
    }
}
