using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Trading.AlertSystem.Data.Models;
using Trading.Data.Infrastructure;

namespace Trading.AlertSystem.Service.Repositories;

/// <summary>
/// 价格告警仓储（使用CosmosDB存储）
/// </summary>
public class PriceAlertRepository : IPriceAlertRepository
{
    private readonly CosmosDbContext _cosmosDb;
    private readonly ILogger<PriceAlertRepository> _logger;
    private Container? _container;

    public PriceAlertRepository(CosmosDbContext cosmosDb, ILogger<PriceAlertRepository> logger)
    {
        _cosmosDb = cosmosDb;
        _logger = logger;
    }

    private async Task<Container> GetContainerAsync()
    {
        if (_container == null)
        {
            await _cosmosDb.InitializeAsync();
            // 获取或创建PriceAlerts容器
            var database = _cosmosDb.BacktestContainer.Database;
            _container = await database.CreateContainerIfNotExistsAsync("PriceAlerts", "/id");
        }
        return _container;
    }

    public async Task<IEnumerable<PriceAlert>> GetAllAsync()
    {
        try
        {
            var container = await GetContainerAsync();
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
            var container = await GetContainerAsync();
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
            var container = await GetContainerAsync();
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
            
            var container = await GetContainerAsync();
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
            
            var container = await GetContainerAsync();
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
            var container = await GetContainerAsync();
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
