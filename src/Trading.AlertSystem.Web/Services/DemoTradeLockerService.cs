using Microsoft.Extensions.Logging;
using Trading.AlertSystem.Data.Models;
using Trading.AlertSystem.Data.Services;

namespace Trading.AlertSystem.Web.Services;

/// <summary>
/// 演示模式的TradeLocker服务（返回模拟数据）
/// </summary>
public class DemoTradeLockerService : ITradeLockerService
{
    private readonly ILogger<DemoTradeLockerService> _logger;
    private readonly Random _random = new();

    public DemoTradeLockerService(ILogger<DemoTradeLockerService> logger)
    {
        _logger = logger;
        _logger.LogWarning("使用演示模式TradeLocker服务 - 返回模拟价格数据。请配置真实的TradeLocker凭证以获取实时数据。");
    }

    public Task<bool> ConnectAsync()
    {
        _logger.LogInformation("[演示模式] TradeLocker连接 - 演示模式总是返回成功");
        return Task.FromResult(true);
    }

    public Task<SymbolPrice?> GetSymbolPriceAsync(string symbol)
    {
        // 为常见交易品种返回模拟价格
        var basePrice = symbol.ToUpper() switch
        {
            "XAUUSD" => 2650.00m,
            "XAGUSD" => 30.50m,
            "EURUSD" => 1.0850m,
            "GBPUSD" => 1.2750m,
            "USDJPY" => 148.50m,
            _ => 100.00m
        };

        // 添加随机波动
        var variation = (decimal)(_random.NextDouble() * 2 - 1) * (basePrice * 0.001m);
        var price = basePrice + variation;

        var symbolPrice = new SymbolPrice
        {
            Symbol = symbol,
            Bid = price - 0.01m,
            Ask = price + 0.01m,
            LastPrice = price,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogDebug("[演示模式] {Symbol} 价格: {Price}", symbol, price);
        return Task.FromResult<SymbolPrice?>(symbolPrice);
    }

    public async Task<IEnumerable<SymbolPrice>> GetSymbolPricesAsync(IEnumerable<string> symbols)
    {
        var tasks = symbols.Select(s => GetSymbolPriceAsync(s));
        var results = await Task.WhenAll(tasks);
        return results.Where(r => r != null).Cast<SymbolPrice>();
    }

    public Task<IEnumerable<Candle>> GetHistoricalDataAsync(string symbol, string timeFrame, int bars)
    {
        _logger.LogDebug("[演示模式] 获取 {Symbol} 历史数据: {TimeFrame}, {Bars}根K线", symbol, timeFrame, bars);

        var basePrice = symbol.ToUpper() switch
        {
            "XAUUSD" => 2650.00m,
            "XAGUSD" => 30.50m,
            "EURUSD" => 1.0850m,
            _ => 100.00m
        };

        var candles = new List<Candle>();
        var currentTime = DateTime.UtcNow;

        // 生成模拟K线数据
        for (int i = bars - 1; i >= 0; i--)
        {
            var open = basePrice + (decimal)(_random.NextDouble() * 4 - 2);
            var close = open + (decimal)(_random.NextDouble() * 2 - 1);
            var high = Math.Max(open, close) + (decimal)(_random.NextDouble() * 1);
            var low = Math.Min(open, close) - (decimal)(_random.NextDouble() * 1);

            candles.Add(new Candle
            {
                Time = currentTime.AddMinutes(-i * 5), // 假设5分钟K线
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = (decimal)(_random.Next(100, 1000))
            });
        }

        return Task.FromResult<IEnumerable<Candle>>(candles);
    }

    public Task<AccountInfo?> GetAccountInfoAsync()
    {
        _logger.LogInformation("[演示模式] 获取账户信息 - 返回模拟数据");

        var accountInfo = new AccountInfo
        {
            AccountId = 123456,
            AccountName = "Demo Account",
            Balance = 10000.00m,
            Equity = 10000.00m,
            Margin = 0.00m,
            FreeMargin = 10000.00m,
            Currency = "USD"
        };

        return Task.FromResult<AccountInfo?>(accountInfo);
    }
}
