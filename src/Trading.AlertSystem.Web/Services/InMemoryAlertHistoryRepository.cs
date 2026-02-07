using Trading.AlertSystem.Data.Models;
using Trading.AlertSystem.Data.Repositories;

namespace Trading.AlertSystem.Web.Services;

/// <summary>
/// 内存版本的告警历史仓储（无需数据库）
/// </summary>
public class InMemoryAlertHistoryRepository : IAlertHistoryRepository
{
    private readonly List<AlertHistory> _history = new();
    private readonly ILogger<InMemoryAlertHistoryRepository> _logger;
    private readonly object _lock = new();

    public InMemoryAlertHistoryRepository(ILogger<InMemoryAlertHistoryRepository> logger)
    {
        _logger = logger;
    }

    public Task SaveAsync(AlertHistory alert)
    {
        lock (_lock)
        {
            alert.Id = Guid.NewGuid().ToString();
            alert.Timestamp = DateTime.UtcNow;
            _history.Add(alert);

            // 只保留最近1000条记录
            if (_history.Count > 1000)
            {
                _history.RemoveAt(0);
            }
        }
        return Task.CompletedTask;
    }

    public Task<IEnumerable<AlertHistory>> GetRecentAsync(int count = 100)
    {
        lock (_lock)
        {
            var recent = _history
                .OrderByDescending(h => h.Timestamp)
                .Take(count)
                .ToList();
            return Task.FromResult<IEnumerable<AlertHistory>>(recent);
        }
    }

    public Task<IEnumerable<AlertHistory>> GetBySymbolAsync(string symbol, DateTime? startDate = null, DateTime? endDate = null)
    {
        lock (_lock)
        {
            var query = _history.Where(h => h.Symbol == symbol);

            if (startDate.HasValue)
                query = query.Where(h => h.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(h => h.Timestamp <= endDate.Value);

            return Task.FromResult<IEnumerable<AlertHistory>>(query.ToList());
        }
    }

    public Task<int> GetCountAsync(DateTime? startDate = null, DateTime? endDate = null)
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
