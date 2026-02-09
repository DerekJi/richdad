using Trading.Infrastructure.Models;
using Trading.Services.Repositories;

namespace Trading.Web.Services;

/// <summary>
/// 内存版本的价格监控仓储（用于演示，无需数据库）
/// </summary>
public class InMemoryPriceMonitorRepository : IPriceMonitorRepository
{
    private readonly Dictionary<string, PriceMonitorRule> _rules = new();
    private readonly ILogger<InMemoryPriceMonitorRepository> _logger;
    private readonly object _lock = new();

    public InMemoryPriceMonitorRepository(ILogger<InMemoryPriceMonitorRepository> logger)
    {
        _logger = logger;
        _logger.LogWarning("使用内存存储 - 数据在应用重启后会丢失。建议配置CosmosDB以持久化数据。");
    }

    public Task<IEnumerable<PriceMonitorRule>> GetAllAsync()
    {
        lock (_lock)
        {
            return Task.FromResult<IEnumerable<PriceMonitorRule>>(_rules.Values.ToList());
        }
    }

    public Task<IEnumerable<PriceMonitorRule>> GetEnabledRulesAsync()
    {
        lock (_lock)
        {
            var enabled = _rules.Values
                .Where(r => r.Enabled && !r.IsTriggered)
                .ToList();
            return Task.FromResult<IEnumerable<PriceMonitorRule>>(enabled);
        }
    }

    public Task<IEnumerable<PriceMonitorRule>> GetTriggeredRulesAsync()
    {
        lock (_lock)
        {
            var triggered = _rules.Values
                .Where(r => r.IsTriggered)
                .ToList();
            return Task.FromResult<IEnumerable<PriceMonitorRule>>(triggered);
        }
    }

    public Task<PriceMonitorRule?> GetByIdAsync(string id)
    {
        lock (_lock)
        {
            _rules.TryGetValue(id, out var rule);
            return Task.FromResult(rule);
        }
    }

    public Task<PriceMonitorRule> CreateAsync(PriceMonitorRule rule)
    {
        lock (_lock)
        {
            rule.Id = Guid.NewGuid().ToString();
            rule.CreatedAt = DateTime.UtcNow;
            rule.UpdatedAt = DateTime.UtcNow;
            _rules[rule.Id] = rule;
            _logger.LogInformation("创建监控规则: {RuleId} - {RuleName}", rule.Id, rule.Name);
            return Task.FromResult(rule);
        }
    }

    public Task<PriceMonitorRule?> UpdateAsync(PriceMonitorRule rule)
    {
        lock (_lock)
        {
            if (!_rules.ContainsKey(rule.Id))
            {
                return Task.FromResult<PriceMonitorRule?>(null);
            }

            rule.UpdatedAt = DateTime.UtcNow;
            _rules[rule.Id] = rule;
            _logger.LogInformation("更新监控规则: {RuleId}", rule.Id);
            return Task.FromResult<PriceMonitorRule?>(rule);
        }
    }

    public Task<bool> DeleteAsync(string id)
    {
        lock (_lock)
        {
            var removed = _rules.Remove(id);
            if (removed)
            {
                _logger.LogInformation("删除监控规则: {RuleId}", id);
            }
            return Task.FromResult(removed);
        }
    }

    public async Task MarkAsTriggeredAsync(string id)
    {
        var rule = await GetByIdAsync(id);
        if (rule != null)
        {
            rule.IsTriggered = true;
            rule.LastTriggeredAt = DateTime.UtcNow;
            await UpdateAsync(rule);
        }
    }

    public async Task ResetRuleAsync(string id)
    {
        var rule = await GetByIdAsync(id);
        if (rule != null)
        {
            rule.IsTriggered = false;
            await UpdateAsync(rule);
        }
    }
}
