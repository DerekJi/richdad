using Trading.Infras.Data.Services;

namespace Trading.Infras.Web;

public class ListSymbolsCommand
{
    private readonly ITradeLockerService _tradeLockerService;

    public ListSymbolsCommand(ITradeLockerService tradeLockerService)
    {
        _tradeLockerService = tradeLockerService;
    }

    public async Task ExecuteAsync()
    {
        Console.WriteLine("=== 获取TradeLocker可用品种列表 ===\n");

        try
        {
            // Connect first
            var connected = await _tradeLockerService.ConnectAsync();
            if (!connected)
            {
                Console.WriteLine("❌ 连接TradeLocker失败");
                return;
            }

            Console.WriteLine("✅ 已连接到TradeLocker\n");

            // Get account info to show balance
            var accountInfo = await _tradeLockerService.GetAccountInfoAsync();
            if (accountInfo != null)
            {
                Console.WriteLine($"账户ID: {accountInfo.AccountId}");
                Console.WriteLine($"余额: {accountInfo.Balance:F2} {accountInfo.Currency}");
                Console.WriteLine($"净值: {accountInfo.Equity:F2} {accountInfo.Currency}\n");
            }

            // Test a few symbols to see what's available
            var testSymbols = new[] {
                "XAGUSD", "XAUUSD", "EURUSD", "GBPUSD", "BTCUSD",
                "ETHUSD", "SOLUSD", "AAVEUSD", "BNBUSD"
            };

            Console.WriteLine("测试品种:");
            foreach (var symbol in testSymbols)
            {
                var price = await _tradeLockerService.GetSymbolPriceAsync(symbol);
                if (price != null)
                {
                    Console.WriteLine($"✅ {symbol,-12} Bid: {price.Bid,10:F4}  Ask: {price.Ask,10:F4}  Mid: {price.LastPrice,10:F4}");
                }
                else
                {
                    Console.WriteLine($"❌ {symbol,-12} 不可用");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 错误: {ex.Message}");
        }
    }
}
