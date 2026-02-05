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
    private Container? _alertHistoryContainer;
    private Container? _emaConfigContainer;
    private Container? _dataSourceConfigContainer;
    private Container? _emailConfigContainer;

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

        _alertHistoryContainer = await _database.CreateContainerIfNotExistsAsync(
            _settings.AlertHistoryContainerName,
            "/symbol");

        _emaConfigContainer = await _database.CreateContainerIfNotExistsAsync(
            _settings.EmaConfigContainerName,
            "/id");

        _dataSourceConfigContainer = await _database.CreateContainerIfNotExistsAsync(
            _settings.DataSourceConfigContainerName,
            "/id");

        _emailConfigContainer = await _database.CreateContainerIfNotExistsAsync(
            _settings.EmailConfigContainerName,
            "/id");
    }

    /// <summary>
    /// 价格提醒容器
    /// </summary>
    public Container AlertContainer
    {
        get => _alertContainer ?? throw new InvalidOperationException("CosmosDbContext未初始化，请先调用InitializeAsync()");
    }

    /// <summary>
    /// 告警历史容器
    /// </summary>
    public Container AlertHistoryContainer
    {
        get => _alertHistoryContainer ?? throw new InvalidOperationException("CosmosDbContext未初始化，请先调用InitializeAsync()");
    }

    /// <summary>
    /// EMA配置容器
    /// </summary>
    public Container EmaConfigContainer
    {
        get => _emaConfigContainer ?? throw new InvalidOperationException("CosmosDbContext未初始化，请先调用InitializeAsync()");
    }

    /// <summary>
    /// 数据源配置容器
    /// </summary>
    public Container DataSourceConfigContainer
    {
        get => _dataSourceConfigContainer ?? throw new InvalidOperationException("CosmosDbContext未初始化，请先调用InitializeAsync()");
    }

    /// <summary>
    /// 邮件配置容器
    /// </summary>
    public Container EmailConfigContainer
    {
        get => _emailConfigContainer ?? throw new InvalidOperationException("CosmosDbContext未初始化，请先调用InitializeAsync()");
    }

    /// <summary>
    /// 获取邮件配置容器
    /// </summary>
    public Container GetEmailConfigContainer() => EmailConfigContainer;

    /// <summary>
    /// 根据容器名称获取容器
    /// </summary>
    public Container GetContainer(string containerName)
    {
        if (_database == null)
            throw new InvalidOperationException("CosmosDbContext未初始化，请先调用InitializeAsync()");

        return containerName switch
        {
            var name when name == _settings.AlertContainerName => AlertContainer,
            var name when name == _settings.AlertHistoryContainerName => AlertHistoryContainer,
            var name when name == _settings.EmaConfigContainerName => EmaConfigContainer,
            var name when name == _settings.DataSourceConfigContainerName => DataSourceConfigContainer,
            var name when name == _settings.EmailConfigContainerName => EmailConfigContainer,
            _ => throw new ArgumentException($"未知的容器名称: {containerName}")
        };
    }
}
