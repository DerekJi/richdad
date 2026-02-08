using Trading.Infras.Data.Models;
using Trading.Infras.Data.Repositories;

namespace Trading.Infras.Web.Services;

/// <summary>
/// 内存版本的PinBar监控仓储（无需数据库）
/// </summary>
public class InMemoryPinBarMonitorRepository : IPinBarMonitorRepository
{
    private PinBarMonitoringConfig? _config;
    private readonly List<PinBarSignalHistory> _signals = new();
    private readonly ILogger<InMemoryPinBarMonitorRepository> _logger;
    private readonly object _lock = new();

    public InMemoryPinBarMonitorRepository(ILogger<InMemoryPinBarMonitorRepository> logger)
    {
        _logger = logger;
    }

    public Task<PinBarMonitoringConfig?> GetConfigAsync()
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
                        RiskRewardRatio = 2.0m
                    }
                };
            }
            return Task.FromResult<PinBarMonitoringConfig?>(_config);
        }
    }

    public Task<PinBarMonitoringConfig> SaveConfigAsync(PinBarMonitoringConfig config)
    {
        lock (_lock)
        {
            _config = config;
            _logger.LogInformation("PinBar配置已更新（内存）");
            return Task.FromResult(config);
        }
    }

    public Task<PinBarSignalHistory> SaveSignalAsync(PinBarSignalHistory signal)
    {
        lock (_lock)
        {
            if (string.IsNullOrEmpty(signal.Id))
                signal.Id = Guid.NewGuid().ToString();
            _signals.Add(signal);

            // 只保留最近500条记录
            if (_signals.Count > 500)
            {
                _signals.RemoveAt(0);
            }

            return Task.FromResult(signal);
        }
    }

    public Task<List<PinBarSignalHistory>> GetRecentSignalsAsync(int count = 100)
    {
        lock (_lock)
        {
            var recent = _signals
                .OrderByDescending(s => s.SignalTime)
                .Take(count)
                .ToList();
            return Task.FromResult(recent);
        }
    }

    public Task<List<PinBarSignalHistory>> GetSignalsBySymbolAsync(string symbol, int count = 50)
    {
        lock (_lock)
        {
            var query = _signals
                .Where(s => s.Symbol == symbol)
                .OrderByDescending(s => s.SignalTime)
                .Take(count)
                .ToList();
            return Task.FromResult(query);
        }
    }

    public Task<PinBarSignalHistory?> GetLastSignalAsync(string symbol, string timeFrame, DateTime afterTime)
    {
        lock (_lock)
        {
            var signal = _signals
                .Where(s => s.Symbol == symbol && s.TimeFrame == timeFrame && s.SignalTime > afterTime)
                .OrderByDescending(s => s.SignalTime)
                .FirstOrDefault();
            return Task.FromResult(signal);
        }
    }
}
