using Trading.AlertSystem.Data.Models;
using Trading.AlertSystem.Service.Repositories;

namespace Trading.AlertSystem.Web.Services;

/// <summary>
/// 内存版本的价格告警仓储（用于演示，无需数据库）
/// </summary>
public class InMemoryPriceAlertRepository : IPriceAlertRepository
{
    private readonly Dictionary<string, PriceAlert> _alerts = new();
    private readonly ILogger<InMemoryPriceAlertRepository> _logger;
    private readonly object _lock = new();

    public InMemoryPriceAlertRepository(ILogger<InMemoryPriceAlertRepository> logger)
    {
        _logger = logger;
        _logger.LogWarning("使用内存存储 - 数据在应用重启后会丢失。建议配置CosmosDB以持久化数据。");
    }

    public Task<IEnumerable<PriceAlert>> GetAllAsync()
    {
        lock (_lock)
        {
            return Task.FromResult<IEnumerable<PriceAlert>>(_alerts.Values.ToList());
        }
    }

    public Task<IEnumerable<PriceAlert>> GetEnabledAlertsAsync()
    {
        lock (_lock)
        {
            var enabled = _alerts.Values
                .Where(a => a.Enabled && !a.IsTriggered)
                .ToList();
            return Task.FromResult<IEnumerable<PriceAlert>>(enabled);
        }
    }

    public Task<IEnumerable<PriceAlert>> GetTriggeredAlertsAsync()
    {
        lock (_lock)
        {
            var triggered = _alerts.Values
                .Where(a => a.IsTriggered)
                .ToList();
            return Task.FromResult<IEnumerable<PriceAlert>>(triggered);
        }
    }

    public Task<PriceAlert?> GetByIdAsync(string id)
    {
        lock (_lock)
        {
            _alerts.TryGetValue(id, out var alert);
            return Task.FromResult(alert);
        }
    }

    public Task<PriceAlert> CreateAsync(PriceAlert alert)
    {
        lock (_lock)
        {
            alert.Id = Guid.NewGuid().ToString();
            alert.CreatedAt = DateTime.UtcNow;
            alert.UpdatedAt = DateTime.UtcNow;
            _alerts[alert.Id] = alert;
            _logger.LogInformation("创建告警: {AlertId} - {AlertName}", alert.Id, alert.Name);
            return Task.FromResult(alert);
        }
    }

    public Task<PriceAlert?> UpdateAsync(PriceAlert alert)
    {
        lock (_lock)
        {
            if (!_alerts.ContainsKey(alert.Id))
            {
                return Task.FromResult<PriceAlert?>(null);
            }

            alert.UpdatedAt = DateTime.UtcNow;
            _alerts[alert.Id] = alert;
            _logger.LogInformation("更新告警: {AlertId}", alert.Id);
            return Task.FromResult<PriceAlert?>(alert);
        }
    }

    public Task<bool> DeleteAsync(string id)
    {
        lock (_lock)
        {
            var removed = _alerts.Remove(id);
            if (removed)
            {
                _logger.LogInformation("删除告警: {AlertId}", id);
            }
            return Task.FromResult(removed);
        }
    }

    public async Task MarkAsTriggeredAsync(string id)
    {
        var alert = await GetByIdAsync(id);
        if (alert != null)
        {
            alert.IsTriggered = true;
            alert.LastTriggeredAt = DateTime.UtcNow;
            await UpdateAsync(alert);
        }
    }

    public async Task ResetAlertAsync(string id)
    {
        var alert = await GetByIdAsync(id);
        if (alert != null)
        {
            alert.IsTriggered = false;
            await UpdateAsync(alert);
        }
    }
}
