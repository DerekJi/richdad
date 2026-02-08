using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Trading.AlertSystem.Data.Configuration;
using Trading.AlertSystem.Data.Infrastructure;
using Trading.AlertSystem.Data.Models;

namespace Trading.AlertSystem.Data.Repositories;

/// <summary>
/// 数据源配置仓储实现
/// </summary>
public class DataSourceConfigRepository : IDataSourceConfigRepository
{
    private readonly Container _container;
    private readonly ILogger<DataSourceConfigRepository> _logger;

    public DataSourceConfigRepository(
        CosmosDbContext context,
        CosmosDbSettings settings,
        ILogger<DataSourceConfigRepository> logger)
    {
        _container = context.GetContainer(settings.DataSourceConfigContainerName);
        _logger = logger;

        // 初始化默认配置
        _ = InitializeDefaultConfigAsync();
    }

    private async Task InitializeDefaultConfigAsync()
    {
        try
        {
            var config = await GetConfigAsync();
            if (config == null)
            {
                _logger.LogInformation("初始化默认数据源配置: Oanda");
                await UpdateConfigAsync(new DataSourceConfig
                {
                    Id = "current",
                    Provider = "Oanda",
                    LastUpdated = DateTime.UtcNow,
                    UpdatedBy = "System"
                });
            }
            else
            {
                _logger.LogInformation("数据源配置已存在: {Provider}", config.Provider);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化数据源配置失败");
        }
    }

    public async Task<DataSourceConfig> GetConfigAsync()
    {
        try
        {
            var response = await _container.ReadItemAsync<DataSourceConfig>(
                "current",
                new PartitionKey("current"));

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("数据源配置不存在，返回默认配置");
            return new DataSourceConfig
            {
                Id = "current",
                Provider = "Oanda",
                LastUpdated = DateTime.UtcNow,
                UpdatedBy = "System"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取数据源配置失败");
            throw;
        }
    }

    public async Task UpdateConfigAsync(DataSourceConfig config)
    {
        try
        {
            config.Id = "current"; // 确保ID固定
            config.LastUpdated = DateTime.UtcNow;

            // 先删除旧配置（如果存在），再创建新配置
            try
            {
                await _container.DeleteItemAsync<DataSourceConfig>("current", new PartitionKey("current"));
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // 配置不存在，忽略
            }

            // 创建新配置
            await _container.CreateItemAsync(config, new PartitionKey(config.Id));

            _logger.LogInformation("数据源配置已更新: {Provider}", config.Provider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新数据源配置失败");
            throw;
        }
    }
}
