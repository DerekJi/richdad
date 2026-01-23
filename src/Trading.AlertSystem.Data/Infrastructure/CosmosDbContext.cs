using Microsoft.Azure.Cosmos;
using Trading.AlertSystem.Data.Configuration;

namespace Trading.AlertSystem.Data.Infrastructure;

/// <summary>
/// Cosmos DB 上下文
/// </summary>
public class CosmosDbContext
{
    private readonly CosmosClient _client;
    private readonly CosmosDbSettings _settings;
    private Database? _database;
    private Container? _alertContainer;

    public CosmosDbContext(CosmosDbSettings settings)
    {
        _settings = settings;
        var clientOptions = new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            HttpClientFactory = () =>
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };
                return new HttpClient(handler);
            }
        };
        _client = new CosmosClient(_settings.ConnectionString, clientOptions);
    }

    /// <summary>
    /// 初始化数据库和容器
    /// </summary>
    public async Task InitializeAsync()
    {
        _database = await _client.CreateDatabaseIfNotExistsAsync(_settings.DatabaseName);

        _alertContainer = await _database.CreateContainerIfNotExistsAsync(
            _settings.AlertContainerName,
            "/id");
    }

    /// <summary>
    /// 价格提醒容器
    /// </summary>
    public Container AlertContainer
    {
        get => _alertContainer ?? throw new InvalidOperationException("CosmosDbContext未初始化，请先调用InitializeAsync()");
    }
}
