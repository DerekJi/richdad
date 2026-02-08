using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Trading.Infras.Data.Infrastructure;
using Trading.Infras.Data.Models;

namespace Trading.Infras.Data.Repositories;

public class PinBarMonitorRepository : IPinBarMonitorRepository
{
    private readonly CosmosDbContext _context;
    private readonly ILogger<PinBarMonitorRepository> _logger;
    private Container ConfigContainer => _context.GetContainer("PinBarMonitorConfig");
    private Container SignalContainer => _context.GetContainer("PinBarSignalHistory");

    public PinBarMonitorRepository(
        CosmosDbContext context,
        ILogger<PinBarMonitorRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PinBarMonitoringConfig?> GetConfigAsync()
    {
        try
        {
            var response = await ConfigContainer.ReadItemAsync<PinBarMonitoringConfig>(
                "default",
                new PartitionKey("default"));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogInformation("PinBar配置不存在，返回默认配置");
            return CreateDefaultConfig();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取PinBar配置失败");
            throw;
        }
    }

    public async Task<PinBarMonitoringConfig> SaveConfigAsync(PinBarMonitoringConfig config)
    {
        try
        {
            config.Id = "default";
            config.UpdatedAt = DateTime.UtcNow;

            var response = await ConfigContainer.UpsertItemAsync(
                config,
                new PartitionKey(config.Id));

            _logger.LogInformation("PinBar配置已保存: Enabled={Enabled}, Symbols={Symbols}",
                config.Enabled, string.Join(",", config.Symbols));

            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存PinBar配置失败");
            throw;
        }
    }

    public async Task<PinBarSignalHistory> SaveSignalAsync(PinBarSignalHistory signal)
    {
        try
        {
            var response = await SignalContainer.CreateItemAsync(
                signal,
                new PartitionKey(signal.Symbol));

            _logger.LogInformation("PinBar信号已保存: {Symbol} {TimeFrame} {Direction}",
                signal.Symbol, signal.TimeFrame, signal.Direction);

            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存PinBar信号失败");
            throw;
        }
    }

    public async Task<List<PinBarSignalHistory>> GetRecentSignalsAsync(int count = 100)
    {
        try
        {
            var query = new QueryDefinition(
                "SELECT * FROM c ORDER BY c.CreatedAt DESC OFFSET 0 LIMIT @count")
                .WithParameter("@count", count);

            var iterator = SignalContainer.GetItemQueryIterator<PinBarSignalHistory>(query);
            var results = new List<PinBarSignalHistory>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取最近PinBar信号失败");
            return new List<PinBarSignalHistory>();
        }
    }

    public async Task<List<PinBarSignalHistory>> GetSignalsBySymbolAsync(string symbol, int count = 50)
    {
        try
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.Symbol = @symbol ORDER BY c.CreatedAt DESC OFFSET 0 LIMIT @count")
                .WithParameter("@symbol", symbol)
                .WithParameter("@count", count);

            var iterator = SignalContainer.GetItemQueryIterator<PinBarSignalHistory>(
                query,
                requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(symbol) });

            var results = new List<PinBarSignalHistory>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取品种PinBar信号失败: {Symbol}", symbol);
            return new List<PinBarSignalHistory>();
        }
    }

    public async Task<PinBarSignalHistory?> GetLastSignalAsync(string symbol, string timeFrame, DateTime afterTime)
    {
        try
        {
            var query = new QueryDefinition(@"
                SELECT * FROM c
                WHERE c.Symbol = @symbol
                  AND c.TimeFrame = @timeFrame
                  AND c.PinBarTime > @afterTime
                ORDER BY c.PinBarTime DESC
                OFFSET 0 LIMIT 1")
                .WithParameter("@symbol", symbol)
                .WithParameter("@timeFrame", timeFrame)
                .WithParameter("@afterTime", afterTime);

            var iterator = SignalContainer.GetItemQueryIterator<PinBarSignalHistory>(
                query,
                requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(symbol) });

            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.FirstOrDefault();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取最后信号失败: {Symbol} {TimeFrame}", symbol, timeFrame);
            return null;
        }
    }

    private PinBarMonitoringConfig CreateDefaultConfig()
    {
        return new PinBarMonitoringConfig
        {
            Id = "default",
            Enabled = false,
            Symbols = new List<string> { "XAUUSD", "XAGUSD" },
            TimeFrames = new List<string> { "M5", "M15", "H1" },
            HistoryMultiplier = 3,
            StrategySettings = new PinBarStrategySettings
            {
                StrategyName = "PinBar",
                BaseEma = 200,
                EmaList = new List<int> { 20, 50, 100 },
                NearEmaThreshold = 0.001m,
                Threshold = 0.0001m,
                MinLowerWickAtrRatio = 1.2m,
                MaxBodyPercentage = 35m,
                MinLongerWickPercentage = 50m,
                MaxShorterWickPercentage = 25m,
                RequirePinBarDirectionMatch = true,
                MinAdx = 0m,
                LowAdxRiskRewardRatio = 0m,
                RiskRewardRatio = 2m,
                NoTradingHoursLimit = true,
                StartTradingHour = 0,
                EndTradingHour = 23,
                StopLossStrategy = "PinbarEndPlusAtr",
                StopLossAtrRatio = 0.3m
            },
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = "System"
        };
    }
}
