using Trading.Infrastructure.Services;

namespace Trading.Web.Services;

/// <summary>
/// TradeLocker启动测试服务
/// </summary>
public class TradeLockerStartupTestService : IHostedService
{
    private readonly ITradeLockerService _tradeLockerService;
    private readonly ILogger<TradeLockerStartupTestService> _logger;

    public TradeLockerStartupTestService(
        ITradeLockerService tradeLockerService,
        ILogger<TradeLockerStartupTestService> logger)
    {
        _tradeLockerService = tradeLockerService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("=== 开始测试TradeLocker连接 ===");

        try
        {
            // 测试连接
            var connected = await _tradeLockerService.ConnectAsync();
            _logger.LogInformation("TradeLocker连接状态: {Connected}", connected);

            if (connected)
            {
                // 测试获取账户信息
                _logger.LogInformation("正在获取账户信息...");
                var accountInfo = await _tradeLockerService.GetAccountInfoAsync();

                if (accountInfo != null)
                {
                    _logger.LogInformation("✅ 账户信息获取成功:");
                    _logger.LogInformation("  账户ID: {AccountId}", accountInfo.AccountId);
                    _logger.LogInformation("  账户名称: {AccountName}", accountInfo.AccountName);
                    _logger.LogInformation("  余额: {Balance} {Currency}", accountInfo.Balance, accountInfo.Currency);
                    _logger.LogInformation("  净值: {Equity} {Currency}", accountInfo.Equity, accountInfo.Currency);
                    _logger.LogInformation("  已用保证金: {Margin} {Currency}", accountInfo.Margin, accountInfo.Currency);
                    _logger.LogInformation("  可用保证金: {FreeMargin} {Currency}", accountInfo.FreeMargin, accountInfo.Currency);

                    // 测试常用品种
                    _logger.LogInformation("\n=== 测试交易品种 ===");
                    var testSymbols = new[] { "XAGUSD", "XAUUSD", "EURUSD", "GBPUSD", "BTCUSD", "ETHUSD", "SOLUSD", "AAVEUSD", "BNBUSD" };
                    foreach (var symbol in testSymbols)
                    {
                        var price = await _tradeLockerService.GetSymbolPriceAsync(symbol);
                        if (price != null)
                        {
                            _logger.LogInformation("✅ {Symbol,-10} Bid: {Bid,10:F4}  Ask: {Ask,10:F4}", symbol, price.Bid, price.Ask);
                        }
                        else
                        {
                            _logger.LogWarning("❌ {Symbol,-10} 不可用", symbol);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("❌ 未能获取账户信息");
                }
            }
            else
            {
                _logger.LogWarning("❌ TradeLocker连接失败");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 测试TradeLocker时发生异常");
        }

        _logger.LogInformation("=== TradeLocker连接测试完成 ===");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
