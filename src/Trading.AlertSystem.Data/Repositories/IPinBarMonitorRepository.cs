using Trading.AlertSystem.Data.Models;

namespace Trading.AlertSystem.Data.Repositories;

public interface IPinBarMonitorRepository
{
    Task<PinBarMonitoringConfig?> GetConfigAsync();
    Task<PinBarMonitoringConfig> SaveConfigAsync(PinBarMonitoringConfig config);
    Task<PinBarSignalHistory> SaveSignalAsync(PinBarSignalHistory signal);
    Task<List<PinBarSignalHistory>> GetRecentSignalsAsync(int count = 100);
    Task<List<PinBarSignalHistory>> GetSignalsBySymbolAsync(string symbol, int count = 50);
    Task<PinBarSignalHistory?> GetLastSignalAsync(string symbol, string timeFrame, DateTime afterTime);
}
