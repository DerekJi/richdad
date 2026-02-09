using Microsoft.Extensions.Logging;
using Trading.Infrastructure.Configuration;
using Trading.Infrastructure.Models;
using Trading.Models;

namespace Trading.Infrastructure.Services;

/// <summary>
/// 市场数据服务实现（根据配置自动路由到TradeLocker或OANDA）
/// </summary>
public class MarketDataService : IMarketDataService
{
    private readonly ITradeLockerService _tradeLockerService;
    private readonly IOandaService? _oandaService;
    private readonly DataSourceSettings _settings;
    private readonly ILogger<MarketDataService> _logger;

    public MarketDataService(
        ITradeLockerService tradeLockerService,
        DataSourceSettings settings,
        ILogger<MarketDataService> logger,
        IOandaService? oandaService = null)
    {
        _tradeLockerService = tradeLockerService;
        _oandaService = oandaService;
        _settings = settings;
        _logger = logger;
    }

    public async Task<bool> ConnectAsync()
    {
        var provider = GetCurrentProvider();
        _logger.LogInformation("使用数据源: {Provider}", provider);

        return provider.ToLower() switch
        {
            "oanda" => _oandaService != null ? await _oandaService.ConnectAsync() : false,
            _ => await _tradeLockerService.ConnectAsync()
        };
    }

    public async Task<SymbolPrice?> GetSymbolPriceAsync(string symbol)
    {
        return GetCurrentProvider().ToLower() switch
        {
            "oanda" => _oandaService != null ? await _oandaService.GetSymbolPriceAsync(symbol) : null,
            _ => await _tradeLockerService.GetSymbolPriceAsync(symbol)
        };
    }

    public async Task<List<Trading.Models.Candle>> GetHistoricalDataAsync(string symbol, string timeFrame, int count)
    {
        var result = GetCurrentProvider().ToLower() switch
        {
            "oanda" => _oandaService != null ? await _oandaService.GetHistoricalDataAsync(symbol, timeFrame, count) : new List<Trading.Models.Candle>(),
            _ => await _tradeLockerService.GetHistoricalDataAsync(symbol, timeFrame, count)
        };

        return result?.ToList() ?? new List<Trading.Models.Candle>();
    }

    public async Task<AccountInfo?> GetAccountInfoAsync()
    {
        return GetCurrentProvider().ToLower() switch
        {
            "oanda" => _oandaService != null ? await _oandaService.GetAccountInfoAsync() : null,
            _ => await _tradeLockerService.GetAccountInfoAsync()
        };
    }

    public string GetCurrentProvider()
    {
        return _settings.Provider ?? "TradeLocker";
    }
}
