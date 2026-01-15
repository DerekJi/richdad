using Microsoft.Azure.Cosmos;
using System.Text.Json;
using Trading.Data.Interfaces;
using Trading.Data.Models;
using Trading.Data.Infrastructure;

namespace Trading.Data.Repositories;

/// <summary>
/// Cosmos DB 回测结果仓储
/// </summary>
public class CosmosBacktestRepository : IBacktestRepository
{
    private readonly Container _container;
    private readonly JsonSerializerOptions _jsonOptions;

    public CosmosBacktestRepository(CosmosDbContext dbContext)
    {
        _container = dbContext.BacktestContainer;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters =
            {
                new DecimalJsonConverter(8),
                new NullableDecimalJsonConverter(8)
            }
        };
    }

    public async Task<string> SaveBacktestResultAsync(BacktestResult result)
    {
        // TODO: 暂时跳过Cosmos DB保存，待问题解决后再启用
        System.Console.WriteLine("\n⊙ 跳过Cosmos DB保存（暂时禁用）");
        return result.Id;
        
        // try
        // {
        //     var response = await _container.UpsertItemAsync(result, new PartitionKey(result.Config.Symbol));
        //     System.Console.WriteLine($"\n✓ 成功保存到Cosmos DB (ID: {response.Resource.Id})");
        //     return response.Resource.Id;
        // }
        // catch (Microsoft.Azure.Cosmos.CosmosException ex)
        // {
        //     System.Console.WriteLine($"\n✗ Cosmos DB保存失败: {ex.Message}");
        //     System.Console.WriteLine($"   StatusCode: {ex.StatusCode}, SubStatusCode: {ex.SubStatusCode}");
        //     if (ex.ResponseBody != null)
        //         System.Console.WriteLine($"   Response: {ex.ResponseBody}");
        //     throw;
        // }
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
}

/// <summary>
/// Cosmos DB 策略配置仓储
/// </summary>
public class CosmosStrategyConfigRepository : IStrategyConfigRepository
{
    private readonly Container _container;

    public CosmosStrategyConfigRepository(CosmosDbContext dbContext)
    {
        _container = dbContext.ConfigContainer;
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
