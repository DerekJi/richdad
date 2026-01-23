using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Trading.AlertSystem.Data.Infrastructure;
using Trading.AlertSystem.Data.Models;

namespace Trading.AlertSystem.Service.Repositories;

/// <summary>
/// 价格告警仓储（使用CosmosDB存储）
/// </summary>
public class PriceAlertRepository : IPriceAlertRepository
{
    private readonly CosmosDbContext _cosmosDb;
    private readonly ILogger<PriceAlertRepository> _logger;
    private bool _initialized;

    public PriceAlertRepository(CosmosDbContext cosmosDb, ILogger<PriceAlertRepository> logger)
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

    public async Task<IEnumerable<PriceAlert>> GetAllAsync()
    {
        try
        {
            await EnsureInitializedAsync();
            var container = _cosmosDb.AlertContainer;
            var query = new QueryDefinition("SELECT * FROM c");
            var iterator = container.GetItemQueryIterator<PriceAlert>(query);

            var results = new List<PriceAlert>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有告警失败");
            return Array.Empty<PriceAlert>();
        }
    }

    public async Task<IEnumerable<PriceAlert>> GetEnabledAlertsAsync()
    {
        try
        {
            await EnsureInitializedAsync();
            var container = _cosmosDb.AlertContainer;
            var query = new QueryDefinition("SELECT * FROM c WHERE c.Enabled = true AND c.IsTriggered = false");
            var iterator = container.GetItemQueryIterator<PriceAlert>(query);

            var results = new List<PriceAlert>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取启用的告警失败");
            return Array.Empty<PriceAlert>();
        }
    }

    public async Task<PriceAlert?> GetByIdAsync(string id)
    {
        try
        {
            await EnsureInitializedAsync();
            var container = _cosmosDb.AlertContainer;
            var response = await container.ReadItemAsync<PriceAlert>(id, new PartitionKey(id));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取告警 {AlertId} 失败", id);
            return null;
        }
    }

    public async Task<PriceAlert> CreateAsync(PriceAlert alert)
    {
        try
        {
            alert.CreatedAt = DateTime.UtcNow;
            alert.UpdatedAt = DateTime.UtcNow;

            await EnsureInitializedAsync();
            var container = _cosmosDb.AlertContainer;
            var response = await container.CreateItemAsync(alert, new PartitionKey(alert.Id));
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建告警失败");
            throw;
        }
    }

    public async Task<PriceAlert?> UpdateAsync(PriceAlert alert)
    {
        try
        {
            alert.UpdatedAt = DateTime.UtcNow;

            await EnsureInitializedAsync();
            var container = _cosmosDb.AlertContainer;
            var response = await container.ReplaceItemAsync(alert, alert.Id, new PartitionKey(alert.Id));
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新告警 {AlertId} 失败", alert.Id);
            return null;
        }
    }

    public async Task<bool> DeleteAsync(string id)
    {
        try
        {
            await EnsureInitializedAsync();
            var container = _cosmosDb.AlertContainer;
            await container.DeleteItemAsync<PriceAlert>(id, new PartitionKey(id));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除告警 {AlertId} 失败", id);
            return false;
        }
    }

    public async Task MarkAsTriggeredAsync(string id)
    {
        try
        {
            var alert = await GetByIdAsync(id);
            if (alert != null)
            {
                alert.IsTriggered = true;
                alert.LastTriggeredAt = DateTime.UtcNow;
                await UpdateAsync(alert);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "标记告警 {AlertId} 已触发失败", id);
        }
    }

    public async Task ResetAlertAsync(string id)
    {
        try
        {
            var alert = await GetByIdAsync(id);
            if (alert != null)
            {
                alert.IsTriggered = false;
                await UpdateAsync(alert);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重置告警 {AlertId} 失败", id);
        }
    }
}
