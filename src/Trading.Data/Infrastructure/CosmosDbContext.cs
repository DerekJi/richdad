using Microsoft.Azure.Cosmos;
using Trading.Data.Configuration;

namespace Trading.Data.Infrastructure;

public class CosmosDbContext
{
    private readonly CosmosClient _client;
    private readonly CosmosDbSettings _settings;
    private Database? _database;
    private Container? _backtestContainer;
    private Container? _configContainer;

    public CosmosDbContext(CosmosDbSettings settings)
    {
        _settings = settings;
        var clientOptions = new CosmosClientOptions
        {
            Serializer = new CustomCosmosSerializer(),
            ConnectionMode = ConnectionMode.Gateway,  // 使用 Gateway 模式以支持更大的文档
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

    public async Task InitializeAsync()
    {
        _database = await _client.CreateDatabaseIfNotExistsAsync(_settings.DatabaseName);

        _backtestContainer = await _database.CreateContainerIfNotExistsAsync(
            _settings.BacktestContainerName,
            "/config/symbol");

        _configContainer = await _database.CreateContainerIfNotExistsAsync(
            _settings.ConfigContainerName,
            "/symbol");
    }

    public Container BacktestContainer
    {
        get => _backtestContainer ?? throw new InvalidOperationException("CosmosDbContext未初始化，请先调用InitializeAsync()");
    }

    public Container ConfigContainer
    {
        get => _configContainer ?? throw new InvalidOperationException("CosmosDbContext未初始化，请先调用InitializeAsync()");
    }
}
