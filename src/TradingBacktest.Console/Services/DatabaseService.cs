using Microsoft.Azure.Cosmos;
using TradingBacktest.Data.Models;
using TradingBacktest.Data.Repositories;

namespace TradingBacktest.Console.Services;

/// <summary>
/// 数据库服务
/// </summary>
public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// 保存回测结果到数据库
    /// </summary>
    public async Task<string> SaveResultAsync(BacktestResult result)
    {
        System.Console.WriteLine("\n正在连接Cosmos DB...");
        
        var cosmosClient = CreateCosmosClient();
        
        // 初始化数据库
        await CosmosBacktestRepository.InitializeDatabaseAsync(cosmosClient);

        // 保存结果
        var repository = new CosmosBacktestRepository(cosmosClient);
        var id = await repository.SaveBacktestResultAsync(result);

        System.Console.WriteLine($"保存成功! ID: {id}");
        return id;
    }

    /// <summary>
    /// 创建Cosmos客户端
    /// </summary>
    private CosmosClient CreateCosmosClient()
    {
        return new CosmosClient(_connectionString, new CosmosClientOptions
        {
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            },
            HttpClientFactory = () =>
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };
                return new HttpClient(handler);
            }
        });
    }
}
