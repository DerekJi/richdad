using Trading.AlertSystem.Data.Models;
using Trading.AlertSystem.Data.Repositories;

namespace Trading.AlertSystem.Web.Services;

/// <summary>
/// 内存版本的EMA监控仓储（无需数据库）
/// </summary>
public class InMemoryEmaMonitorRepository : IEmaMonitorRepository
{
    private EmaMonitoringConfig? _config;
    private readonly ILogger<InMemoryEmaMonitorRepository> _logger;
    private readonly object _lock = new();

    public InMemoryEmaMonitorRepository(ILogger<InMemoryEmaMonitorRepository> logger)
    {
        _logger = logger;
    }

    public Task<EmaMonitoringConfig?> GetConfigAsync()
    {
        lock (_lock)
        {
            if (_config == null)
            {
                _config = new EmaMonitoringConfig
                {
                    Id = "default",
                    Enabled = true,
                    Symbols = new List<string> { "XAUUSD", "XAGUSD" },
                    TimeFrames = new List<string> { "M5", "M15", "H1" },
                    EmaPeriods = new List<int> { 20 },
                    HistoryMultiplier = 3
                };
            }
            return Task.FromResult<EmaMonitoringConfig?>(_config);
        }
    }

    public Task<EmaMonitoringConfig> SaveConfigAsync(EmaMonitoringConfig config)
    {
        lock (_lock)
        {
            _config = config;
            _logger.LogInformation("EMA配置已更新（内存）");
            return Task.FromResult(config);
        }
    }

    public Task<EmaMonitoringConfig> InitializeDefaultConfigAsync(bool enabled, List<string> symbols, List<string> timeFrames, List<int> emaPeriods, int historyMultiplier)
    {
        lock (_lock)
        {
            if (_config == null)
            {
                _config = new EmaMonitoringConfig
                {
                    Id = "default",
                    Enabled = enabled,
                    Symbols = symbols,
                    TimeFrames = timeFrames,
                    EmaPeriods = emaPeriods,
                    HistoryMultiplier = historyMultiplier
                };
                _logger.LogInformation("EMA默认配置已初始化（内存）");
            }
            return Task.FromResult(_config);
        }
    }

    public Task DeleteConfigAsync()
    {
        lock (_lock)
        {
            _config = null;
            _logger.LogInformation("EMA配置已删除（内存）");
            return Task.CompletedTask;
        }
    }
}
