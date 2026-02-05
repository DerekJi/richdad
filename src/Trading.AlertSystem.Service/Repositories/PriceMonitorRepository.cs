using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Trading.AlertSystem.Data.Infrastructure;
using Trading.AlertSystem.Data.Models;

namespace Trading.AlertSystem.Service.Repositories;

/// <summary>
/// 价格监控仓储（使用CosmosDB存储）
/// </summary>
public class PriceMonitorRepository : IPriceMonitorRepository
{
    private readonly CosmosDbContext _cosmosDb;
    private readonly ILogger<PriceMonitorRepository> _logger;
    private bool _initialized;

    public PriceMonitorRepository(CosmosDbContext cosmosDb, ILogger<PriceMonitorRepository> logger)
    {
        _cosmosDb = cosmosDb;
        _logger = logger;
    }

    private async Task EnsureInitializedAsync()
    {
        if (!_initialized)
        {
            await _cosmosDb.InitializeAsync();
            _initialized = true;
        }
    }

    public async Task<IEnumerable<PriceMonitorRule>> GetAllAsync()
    {
        try
        {
            await EnsureInitializedAsync();
            var container = _cosmosDb.PriceMonitorContainer;
            var query = new QueryDefinition("SELECT * FROM c");
            var iterator = container.GetItemQueryIterator<PriceMonitorRule>(query);

            var results = new List<PriceMonitorRule>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有监控规则失败");
            return Array.Empty<PriceMonitorRule>();
        }
    }

    public async Task<IEnumerable<PriceMonitorRule>> GetEnabledRulesAsync()
    {
        try
        {
            await EnsureInitializedAsync();
            var container = _cosmosDb.PriceMonitorContainer;
            var query = new QueryDefinition("SELECT * FROM c WHERE c.Enabled = true AND c.IsTriggered = false");
            var iterator = container.GetItemQueryIterator<PriceMonitorRule>(query);

            var results = new List<PriceMonitorRule>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取启用的监控规则失败");
            return Array.Empty<PriceMonitorRule>();
        }
    }

    public async Task<IEnumerable<PriceMonitorRule>> GetTriggeredRulesAsync()
    {
        try
        {
            await EnsureInitializedAsync();
            var container = _cosmosDb.PriceMonitorContainer;
            var query = new QueryDefinition("SELECT * FROM c WHERE c.IsTriggered = true");
            var iterator = container.GetItemQueryIterator<PriceMonitorRule>(query);

            var results = new List<PriceMonitorRule>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取已触发的监控规则失败");
            return Array.Empty<PriceMonitorRule>();
        }
    }

    public async Task<PriceMonitorRule?> GetByIdAsync(string id)
    {
        try
        {
            await EnsureInitializedAsync();
            var container = _cosmosDb.PriceMonitorContainer;
            var response = await container.ReadItemAsync<PriceMonitorRule>(id, new PartitionKey(id));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取监控规则 {RuleId} 失败", id);
            return null;
        }
    }

    public async Task<PriceMonitorRule> CreateAsync(PriceMonitorRule rule)
    {
        try
        {
            rule.CreatedAt = DateTime.UtcNow;
            rule.UpdatedAt = DateTime.UtcNow;

            await EnsureInitializedAsync();
            var container = _cosmosDb.PriceMonitorContainer;
            var response = await container.CreateItemAsync(rule, new PartitionKey(rule.Id));
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建监控规则失败");
            throw;
        }
    }

    public async Task<PriceMonitorRule?> UpdateAsync(PriceMonitorRule rule)
    {
        try
        {
            rule.UpdatedAt = DateTime.UtcNow;

            await EnsureInitializedAsync();
            var container = _cosmosDb.PriceMonitorContainer;
            var response = await container.ReplaceItemAsync(rule, rule.Id, new PartitionKey(rule.Id));
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新监控规则 {RuleId} 失败", rule.Id);
            return null;
        }
    }

    public async Task<bool> DeleteAsync(string id)
    {
        try
        {
            await EnsureInitializedAsync();
            var container = _cosmosDb.PriceMonitorContainer;
            await container.DeleteItemAsync<PriceMonitorRule>(id, new PartitionKey(id));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除监控规则 {RuleId} 失败", id);
            return false;
        }
    }

    public async Task MarkAsTriggeredAsync(string id)
    {
        try
        {
            var rule = await GetByIdAsync(id);
            if (rule != null)
            {
                rule.IsTriggered = true;
                rule.LastTriggeredAt = DateTime.UtcNow;
                await UpdateAsync(rule);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "标记监控规则 {RuleId} 已触发失败", id);
        }
    }

    public async Task ResetRuleAsync(string id)
    {
        try
        {
            var rule = await GetByIdAsync(id);
            if (rule != null)
            {
                rule.IsTriggered = false;
                await UpdateAsync(rule);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重置监控规则 {RuleId} 失败", id);
        }
    }
}
