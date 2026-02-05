using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Trading.AlertSystem.Data.Infrastructure;
using Trading.AlertSystem.Data.Models;

namespace Trading.AlertSystem.Data.Repositories;

/// <summary>
/// EMA监测配置仓储实现
/// </summary>
public class EmaConfigRepository : IEmaConfigRepository
{
    private readonly CosmosDbContext _context;
    private readonly ILogger<EmaConfigRepository> _logger;
    private const string ConfigId = "default";

    public EmaConfigRepository(
        CosmosDbContext context,
        ILogger<EmaConfigRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<EmaMonitoringConfig?> GetConfigAsync()
    {
        try
        {
            var response = await _context.EmaConfigContainer.ReadItemAsync<EmaMonitoringConfig>(
                ConfigId,
                new PartitionKey(ConfigId));

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogInformation("EMA配置不存在，需要初始化");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取EMA配置失败");
            throw;
        }
    }

    public async Task<EmaMonitoringConfig> SaveConfigAsync(EmaMonitoringConfig config)
    {
        try
        {
            config.Id = ConfigId;
            config.UpdatedAt = DateTime.UtcNow;

            var response = await _context.EmaConfigContainer.UpsertItemAsync(
                config,
                new PartitionKey(ConfigId));

            _logger.LogInformation("EMA配置已保存：Enabled={Enabled}, Symbols={SymbolCount}, TimeFrames={TimeFrameCount}",
                config.Enabled, config.Symbols.Count, config.TimeFrames.Count);

            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存EMA配置失败");
            throw;
        }
    }

    public async Task<EmaMonitoringConfig> InitializeDefaultConfigAsync(
        bool enabled,
        List<string> symbols,
        List<string> timeFrames,
        List<int> emaPeriods,
        int historyMultiplier)
    {
        var existingConfig = await GetConfigAsync();
        if (existingConfig != null)
        {
            _logger.LogInformation("EMA配置已存在，跳过初始化");
            return existingConfig;
        }

        var defaultConfig = new EmaMonitoringConfig
        {
            Id = ConfigId,
            Enabled = enabled,
            Symbols = symbols ?? new List<string>(),
            TimeFrames = timeFrames ?? new List<string>(),
            EmaPeriods = emaPeriods ?? new List<int>(),
            HistoryMultiplier = historyMultiplier,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = "System"
        };

        _logger.LogInformation("初始化默认EMA配置：Enabled={Enabled}, Symbols={SymbolCount}",
            defaultConfig.Enabled, defaultConfig.Symbols.Count);

        return await SaveConfigAsync(defaultConfig);
    }

    public async Task DeleteConfigAsync()
    {
        try
        {
            await _context.EmaConfigContainer.DeleteItemAsync<EmaMonitoringConfig>(
                ConfigId,
                new PartitionKey(ConfigId));

            _logger.LogInformation("EMA配置已删除");
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("EMA配置不存在，无需删除");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除EMA配置失败");
            throw;
        }
    }
}
