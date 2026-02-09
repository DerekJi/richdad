using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Trading.Infrastructure.Configuration;

namespace Trading.Infrastructure.Infrastructure;

/// <summary>
/// Azure Table Storage 上下文
/// </summary>
public class AzureTableStorageContext
{
    private readonly AzureTableStorageSettings _settings;
    private readonly TableServiceClient _serviceClient;
    private readonly ILogger<AzureTableStorageContext> _logger;

    public AzureTableStorageContext(
        AzureTableStorageSettings settings,
        ILogger<AzureTableStorageContext> logger)
    {
        _settings = settings;
        _logger = logger;

        try
        {
            _serviceClient = new TableServiceClient(_settings.ConnectionString);
            _logger.LogInformation("✅ Azure Table Storage 客户端已初始化");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 初始化 Azure Table Storage 客户端失败");
            throw;
        }
    }

    /// <summary>
    /// 获取表客户端
    /// </summary>
    public TableClient GetTableClient(string tableName)
    {
        return _serviceClient.GetTableClient(tableName);
    }

    /// <summary>
    /// 初始化所有表
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("🔄 开始初始化 Azure Table Storage 表...");

            var tables = new[]
            {
                _settings.PriceMonitorTableName,
                _settings.AlertHistoryTableName,
                _settings.EmaMonitorTableName,
                _settings.DataSourceConfigTableName,
                _settings.EmailConfigTableName,
                _settings.PinBarMonitorTableName,
                _settings.PinBarSignalTableName,
                _settings.AIAnalysisHistoryTableName
            };

            foreach (var tableName in tables)
            {
                await CreateTableIfNotExistsAsync(tableName);
            }

            _logger.LogInformation("✅ Azure Table Storage 表初始化完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 初始化 Azure Table Storage 表失败");
            throw;
        }
    }

    /// <summary>
    /// 创建表（如果不存在）
    /// </summary>
    private async Task CreateTableIfNotExistsAsync(string tableName)
    {
        try
        {
            var tableClient = GetTableClient(tableName);
            await tableClient.CreateIfNotExistsAsync();
            _logger.LogInformation("✅ 表已创建或已存在: {TableName}", tableName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建表失败: {TableName}", tableName);
            throw;
        }
    }

    /// <summary>
    /// 删除表
    /// </summary>
    public async Task DeleteTableAsync(string tableName)
    {
        try
        {
            await _serviceClient.DeleteTableAsync(tableName);
            _logger.LogInformation("✅ 表已删除: {TableName}", tableName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除表失败: {TableName}", tableName);
            throw;
        }
    }
}
