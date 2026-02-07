using Trading.AlertSystem.Data.Models;
using Trading.AlertSystem.Service.Repositories;

namespace Trading.AlertSystem.Web.Services;

/// <summary>
/// 内存版本的PinBar监控仓储（无需数据库）
/// </summary>
public class InMemoryPinBarMonitorRepository : IPinBarMonitorRepository
{
    private PinBarMonitoringConfig? _config;
    private readonly List<PinBarSignal> _signals = new();
    private readonly ILogger<InMemoryPinBarMonitorRepository> _logger;
    private readonly object _lock = new();

    public InMemoryPinBarMonitorRepository(ILogger<InMemoryPinBarMonitorRepository> logger)
    {
        _logger = logger;
    }

    public Task<PinBarMonitoringConfig> GetConfigAsync()
    {
        lock (_lock)
        {
            if (_config == null)
            {
                _config = new PinBarMonitoringConfig
                {
                    Id = "default",
                    Enabled = true,
                    Symbols = new List<string> { "XAUUSD", "XAGUSD" },
                    TimeFrames = new List<string> { "M15", "H1" },
                    StrategySettings = new PinBarStrategySettings
                    {
                        MinBodyToWickRatio = 0.33m,
                        MinWickToBodyRatio = 2.0m,
                        MaxBodySize = 0.3m,
                        RiskRewardRatio = 2.0m,
                        UseAdxFilter = false,
                        MinAdx = 20m,
                        AdxPeriod = 14
                    }
                };
            }
            return Task.FromResult(_config);
        }
    }

    public Task<PinBarMonitoringConfig> UpdateConfigAsync(PinBarMonitoringConfig config)
    {
        lock (_lock)
        {
            _config = config;
            _logger.LogInformation("PinBar配置已更新（内存）");
            return Task.FromResult(config);
        }
    }

    public Task SaveSignalAsync(PinBarSignal signal)
    {
        lock (_lock)
        {
            _signals.Add(signal);

            // 只保留最近500条记录
            if (_signals.Count > 500)
            {
                _signals.RemoveAt(0);
            }

            return Task.CompletedTask;
        }
    }

    public Task<IEnumerable<PinBarSignal>> GetRecentSignalsAsync(int count = 100)
    {
        lock (_lock)
        {
            var recent = _signals
                .OrderByDescending(s => s.SignalTime)
                .Take(count)
                .ToList();
            return Task.FromResult<IEnumerable<PinBarSignal>>(recent);
        }
    }

    public Task<IEnumerable<PinBarSignal>> GetSignalsBySymbolAsync(string symbol, DateTime? startDate = null, DateTime? endDate = null)
    {
        lock (_lock)
        {
            var query = _signals.Where(s => s.Symbol == symbol);

            if (startDate.HasValue)
                query = query.Where(s => s.SignalTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(s => s.SignalTime <= endDate.Value);

            return Task.FromResult<IEnumerable<PinBarSignal>>(query.ToList());
        }
    }
}
