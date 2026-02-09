using Trading.Infrastructure.Models;
using Trading.Infrastructure.Repositories;

namespace Trading.Web.Services;

/// <summary>
/// 内存版本的数据源配置仓储（无需数据库）
/// </summary>
public class InMemoryDataSourceConfigRepository : IDataSourceConfigRepository
{
    private DataSourceConfig? _config;
    private readonly ILogger<InMemoryDataSourceConfigRepository> _logger;
    private readonly object _lock = new();

    public InMemoryDataSourceConfigRepository(ILogger<InMemoryDataSourceConfigRepository> logger)
    {
        _logger = logger;
    }

    public Task<DataSourceConfig> GetConfigAsync()
    {
        lock (_lock)
        {
            if (_config == null)
            {
                _config = new DataSourceConfig
                {
                    Id = "default",
                    Provider = "Oanda"
                };
            }
            return Task.FromResult(_config);
        }
    }

    public Task UpdateConfigAsync(DataSourceConfig config)
    {
        lock (_lock)
        {
            _config = config;
            _logger.LogInformation("数据源配置已更新为: {Provider}（内存）", config.Provider);
            return Task.CompletedTask;
        }
    }

    public Task InitializeDefaultConfigAsync(string provider = "Oanda")
    {
        lock (_lock)
        {
            if (_config == null)
            {
                _config = new DataSourceConfig
                {
                    Id = "default",
                    Provider = provider
                };
                _logger.LogInformation("数据源默认配置已初始化: {Provider}（内存）", provider);
            }
            return Task.CompletedTask;
        }
    }
}
