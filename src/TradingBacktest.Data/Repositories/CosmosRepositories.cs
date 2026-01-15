using Microsoft.Azure.Cosmos;
using System.Text.Json;
using TradingBacktest.Data.Interfaces;
using TradingBacktest.Data.Models;

namespace TradingBacktest.Data.Repositories;

/// <summary>
/// Cosmos DB 回测结果仓储
/// </summary>
public class CosmosBacktestRepository : IBacktestRepository
{
    private readonly Container _container;
    private readonly JsonSerializerOptions _jsonOptions;

    public CosmosBacktestRepository(CosmosClient cosmosClient, string databaseName = "TradingBacktest", string containerName = "BacktestResults")
    {
        var database = cosmosClient.GetDatabase(databaseName);
        _container = database.GetContainer(containerName);
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    public async Task<string> SaveBacktestResultAsync(BacktestResult result)
    {
        var response = await _container.UpsertItemAsync(result, new PartitionKey(result.Config.Symbol));
        return response.Resource.Id;
    }

    public async Task<BacktestResult?> GetBacktestResultAsync(string id)
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                .WithParameter("@id", id);
            
            var iterator = _container.GetItemQueryIterator<BacktestResult>(query);
            var results = new List<BacktestResult>();
            
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }
            
            return results.FirstOrDefault();
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<List<BacktestResult>> GetAllBacktestResultsAsync(string? symbol = null, int limit = 100)
    {
        var queryText = symbol != null
            ? "SELECT TOP @limit * FROM c WHERE c.config.symbol = @symbol ORDER BY c.backtestTime DESC"
            : "SELECT TOP @limit * FROM c ORDER BY c.backtestTime DESC";
        
        var query = new QueryDefinition(queryText)
            .WithParameter("@limit", limit);
        
        if (symbol != null)
        {
            query = query.WithParameter("@symbol", symbol);
        }
        
        var iterator = _container.GetItemQueryIterator<BacktestResult>(query);
        var results = new List<BacktestResult>();
        
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        
        return results;
    }

    public async Task DeleteBacktestResultAsync(string id)
    {
        // 需要先查询以获取partition key
        var result = await GetBacktestResultAsync(id);
        if (result != null)
        {
            await _container.DeleteItemAsync<BacktestResult>(id, new PartitionKey(result.Config.Symbol));
        }
    }

    /// <summary>
    /// 初始化数据库和容器
    /// </summary>
    public static async Task InitializeDatabaseAsync(CosmosClient cosmosClient, string databaseName = "TradingBacktest")
    {
        // 创建数据库
        var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);
        
        // 创建回测结果容器
        await database.Database.CreateContainerIfNotExistsAsync(
            new ContainerProperties("BacktestResults", "/config/symbol")
            {
                DefaultTimeToLive = -1 // 不过期
            });
        
        // 创建配置容器
        await database.Database.CreateContainerIfNotExistsAsync(
            new ContainerProperties("StrategyConfigs", "/symbol")
            {
                DefaultTimeToLive = -1
            });
    }
}

/// <summary>
/// Cosmos DB 策略配置仓储
/// </summary>
public class CosmosStrategyConfigRepository : IStrategyConfigRepository
{
    private readonly Container _container;

    public CosmosStrategyConfigRepository(CosmosClient cosmosClient, string databaseName = "TradingBacktest", string containerName = "StrategyConfigs")
    {
        var database = cosmosClient.GetDatabase(databaseName);
        _container = database.GetContainer(containerName);
    }

    public async Task<string> SaveConfigAsync(StrategyConfig config)
    {
        // 为配置生成ID
        var configWithId = new ConfigDocument
        {
            Id = Guid.NewGuid().ToString(),
            Config = config,
            CreatedAt = DateTime.UtcNow
        };
        
        var response = await _container.UpsertItemAsync(configWithId, new PartitionKey(config.Symbol));
        return response.Resource.Id;
    }

    public async Task<StrategyConfig?> GetConfigAsync(string id)
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                .WithParameter("@id", id);
            
            var iterator = _container.GetItemQueryIterator<ConfigDocument>(query);
            var results = new List<ConfigDocument>();
            
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }
            
            return results.FirstOrDefault()?.Config;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<List<StrategyConfig>> GetAllConfigsAsync(string? symbol = null)
    {
        var queryText = symbol != null
            ? "SELECT * FROM c WHERE c.config.symbol = @symbol ORDER BY c.createdAt DESC"
            : "SELECT * FROM c ORDER BY c.createdAt DESC";
        
        var query = new QueryDefinition(queryText);
        
        if (symbol != null)
        {
            query = query.WithParameter("@symbol", symbol);
        }
        
        var iterator = _container.GetItemQueryIterator<ConfigDocument>(query);
        var results = new List<StrategyConfig>();
        
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response.Select(d => d.Config));
        }
        
        return results;
    }

    private class ConfigDocument
    {
        public string Id { get; set; } = string.Empty;
        public StrategyConfig Config { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }
}
