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
    private Container? _priceMonitorContainer;
    private Container? _emaAlertsContainer;
    private Container? _emaMonitorContainer;
    private Container? _dataSourceConfigContainer;
    private Container? _emailConfigContainer;
    private Container? _pinBarMonitorConfigContainer;
    private Container? _pinBarSignalHistoryContainer;

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

        _priceMonitorContainer = await _database.CreateContainerIfNotExistsAsync(
            _settings.PriceMonitorContainerName,
            "/id");

        _emaAlertsContainer = await _database.CreateContainerIfNotExistsAsync(
            _settings.AlertHistoryContainerName,
            "/symbol");

        _emaMonitorContainer = await _database.CreateContainerIfNotExistsAsync(
            _settings.EmaMonitorContainerName,
            "/id");

        _dataSourceConfigContainer = await _database.CreateContainerIfNotExistsAsync(
            _settings.DataSourceConfigContainerName,
            "/id");

        _emailConfigContainer = await _database.CreateContainerIfNotExistsAsync(
            _settings.EmailConfigContainerName,
            "/id");

        _pinBarMonitorConfigContainer = await _database.CreateContainerIfNotExistsAsync(
            "PinBarMonitorConfig",
            "/id");

        _pinBarSignalHistoryContainer = await _database.CreateContainerIfNotExistsAsync(
            "PinBarSignalHistory",
            "/symbol");
    }

    /// <summary>
    /// 价格监控容器
    /// </summary>
    public Container PriceMonitorContainer
    {
        get => _priceMonitorContainer ?? throw new InvalidOperationException("CosmosDbContext未初始化，请先调用InitializeAsync()");
    }

    /// <summary>
    /// EMA告警容器
    /// </summary>
    public Container EmaAlertsContainer
    {
        get => _emaAlertsContainer ?? throw new InvalidOperationException("CosmosDbContext未初始化，请先调用InitializeAsync()");
    }

    /// <summary>
    /// EMA监控配置容器
    /// </summary>
    public Container EmaMonitorContainer
    {
        get => _emaMonitorContainer ?? throw new InvalidOperationException("CosmosDbContext未初始化，请先调用InitializeAsync()");
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
            var name when name == _settings.PriceMonitorContainerName => PriceMonitorContainer,
            var name when name == _settings.AlertHistoryContainerName => EmaAlertsContainer,
            var name when name == _settings.EmaMonitorContainerName => EmaMonitorContainer,
            var name when name == _settings.DataSourceConfigContainerName => DataSourceConfigContainer,
            var name when name == _settings.EmailConfigContainerName => EmailConfigContainer,
            "PinBarMonitorConfig" => _pinBarMonitorConfigContainer ?? throw new InvalidOperationException("PinBarMonitorConfig容器未初始化"),
            "PinBarSignalHistory" => _pinBarSignalHistoryContainer ?? throw new InvalidOperationException("PinBarSignalHistory容器未初始化"),
            _ => throw new ArgumentException($"未知的容器名称: {containerName}")
        };
    }
}
