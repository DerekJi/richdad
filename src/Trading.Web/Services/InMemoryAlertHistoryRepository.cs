using Trading.Infrastructure.Models;
using Trading.Infrastructure.Repositories;

namespace Trading.Web.Services;

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

    public Task<AlertHistory> AddAsync(AlertHistory alert)
    {
        lock (_lock)
        {
            if (string.IsNullOrEmpty(alert.Id))
                alert.Id = Guid.NewGuid().ToString();
            if (alert.AlertTime == default)
                alert.AlertTime = DateTime.UtcNow;
            _history.Add(alert);

            // 只保留最近1000条记录
            if (_history.Count > 1000)
            {
                _history.RemoveAt(0);
            }
        }
        return Task.FromResult(alert);
    }

    public Task<AlertHistory?> GetByIdAsync(string id)
    {
        lock (_lock)
        {
            var alert = _history.FirstOrDefault(h => h.Id == id);
            return Task.FromResult(alert);
        }
    }

    public Task<(IEnumerable<AlertHistory> Items, int TotalCount)> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 50,
        AlertHistoryType? type = null,
        string? symbol = null,
        DateTime? startTime = null,
        DateTime? endTime = null)
    {
        lock (_lock)
        {
            var query = _history.AsEnumerable();

            if (type.HasValue)
                query = query.Where(h => h.Type == type.Value);

            if (!string.IsNullOrEmpty(symbol))
                query = query.Where(h => h.Symbol == symbol);

            if (startTime.HasValue)
                query = query.Where(h => h.AlertTime >= startTime.Value);

            if (endTime.HasValue)
                query = query.Where(h => h.AlertTime <= endTime.Value);

            var totalCount = query.Count();
            var items = query
                .OrderByDescending(h => h.AlertTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Task.FromResult((Items: (IEnumerable<AlertHistory>)items, TotalCount: totalCount));
        }
    }

    public Task<IEnumerable<AlertHistory>> GetRecentAsync(int count = 100)
    {
        lock (_lock)
        {
            var recent = _history
                .OrderByDescending(h => h.AlertTime)
                .Take(count)
                .ToList();
            return Task.FromResult<IEnumerable<AlertHistory>>(recent);
        }
    }

    public Task<IEnumerable<AlertHistory>> GetBySymbolAsync(string symbol, int limit = 100)
    {
        lock (_lock)
        {
            var query = _history
                .Where(h => h.Symbol == symbol)
                .OrderByDescending(h => h.AlertTime)
                .Take(limit)
                .ToList();
            return Task.FromResult<IEnumerable<AlertHistory>>(query);
        }
    }

    public Task<IEnumerable<AlertHistory>> GetByTypeAsync(AlertHistoryType type, int limit = 100)
    {
        lock (_lock)
        {
            var query = _history
                .Where(h => h.Type == type)
                .OrderByDescending(h => h.AlertTime)
                .Take(limit)
                .ToList();
            return Task.FromResult<IEnumerable<AlertHistory>>(query);
        }
    }

    public Task<int> DeleteOldRecordsAsync(DateTime beforeDate)
    {
        lock (_lock)
        {
            var count = _history.RemoveAll(h => h.AlertTime < beforeDate);
            return Task.FromResult(count);
        }
    }
}
