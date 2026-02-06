using Microsoft.Extensions.Configuration;
using Trading.Core.RiskManagement;
using Trading.Core.RiskManagement.Configuration;
using Trading.Core.RiskManagement.Models;

namespace Trading.Backtest.Console;

/// <summary>
/// Console command for testing position calculation
/// </summary>
public class PositionCalcCommand
{
    public static async Task RunAsync(string[] args)
    {
        System.Console.WriteLine("=== Position Calculator Test ===\n");

        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        // Load configurations
        var propFirmSettings = new Dictionary<string, PropFirmRuleConfig>();
        configuration.GetSection("PropFirmRules").Bind(propFirmSettings);

        var instrumentSettings = new Dictionary<string, BrokerInstruments>();
        configuration.GetSection("InstrumentSpecifications").Bind(instrumentSettings);

        // Initialize RiskManager
        var riskManager = new RiskManager();

        // Register prop firm rules
        foreach (var (key, config) in propFirmSettings)
        {
            riskManager.RegisterPropFirmRule(config.ToModel());
            System.Console.WriteLine($"Loaded Prop Firm Rule: {config.Name} (Daily: {config.MaxDailyLossPercent}%, Total: {config.MaxTotalLossPercent}%)");
        }

        System.Console.WriteLine();

        // Register instrument specifications
        foreach (var (broker, brokerInstruments) in instrumentSettings)
        {
            foreach (var (symbol, config) in brokerInstruments.Instruments)
            {
                var spec = config.ToModel(broker);
                riskManager.RegisterInstrumentSpec(spec);
                System.Console.WriteLine($"Loaded Instrument: {broker}:{symbol} (Contract Size: {config.ContractSize}, Tick: {config.TickSize})");
            }
        }

        System.Console.WriteLine("\n=== Test Scenarios ===\n");

        // Scenario 1: Blue Guardian - Normal trade
        await TestScenario1(riskManager);

        // Scenario 2: Near daily limit
        await TestScenario2(riskManager);

        // Scenario 3: Custom rules
        await TestScenario3(riskManager);

        // Scenario 4: Different instruments
        await TestScenario4(riskManager);

        System.Console.WriteLine("\n=== Tests Complete ===");
    }

    private static async Task TestScenario1(RiskManager riskManager)
    {
        System.Console.WriteLine("Scenario 1: Blue Guardian - Normal XAUUSD Trade");
        System.Console.WriteLine("Account: $10,000 | Entry: 2650.50 | Stop Loss: 2645.00");

        var riskParams = new RiskParameters
        {
            AccountBalance = 10000m,
            InitialBalance = 10000m,
            PropFirmRule = "BlueGuardian",
            RiskPercentPerTrade = 1.0m,
            TodayLoss = 0m,
            TotalLoss = 0m,
            LastResetDate = DateTime.UtcNow.Date.AddDays(-1)
        };

        var result = riskManager.CalculatePosition(
            symbol: "XAUUSD",
            broker: "ICMarkets",
            entryPrice: 2650.50m,
            stopLoss: 2645.00m,
            riskParams: riskParams
        );

        PrintResult(result);
        await Task.CompletedTask;
    }

    private static async Task TestScenario2(RiskManager riskManager)
    {
        System.Console.WriteLine("\nScenario 2: Near Daily Loss Limit");
        System.Console.WriteLine("Account: $10,000 | Today's Loss: $280 (Daily Limit: $300)");

        var riskParams = new RiskParameters
        {
            AccountBalance = 9720m,
            InitialBalance = 10000m,
            PropFirmRule = "BlueGuardian",
            RiskPercentPerTrade = 1.0m,
            TodayLoss = 280m,
            TotalLoss = 280m,
            LastResetDate = DateTime.UtcNow.Date
        };

        var result = riskManager.CalculatePosition(
            symbol: "XAUUSD",
            broker: "ICMarkets",
            entryPrice: 2650.50m,
            stopLoss: 2645.00m,
            riskParams: riskParams
        );

        PrintResult(result);
        await Task.CompletedTask;
    }

    private static async Task TestScenario3(RiskManager riskManager)
    {
        System.Console.WriteLine("\nScenario 3: Custom Risk Rules (2% daily, 4% total)");
        System.Console.WriteLine("Account: $10,000 | Entry: 2650.50 | Stop Loss: 2645.00");

        var riskParams = new RiskParameters
        {
            AccountBalance = 10000m,
            InitialBalance = 10000m,
            PropFirmRule = null,
            CustomDailyLossPercent = 2.0m,
            CustomTotalLossPercent = 4.0m,
            CustomServerTimeZoneOffset = 2,
            RiskPercentPerTrade = 1.0m,
            TodayLoss = 0m,
            TotalLoss = 0m,
            LastResetDate = DateTime.UtcNow.Date.AddDays(-1)
        };

        var result = riskManager.CalculatePosition(
            symbol: "XAUUSD",
            broker: "ICMarkets",
            entryPrice: 2650.50m,
            stopLoss: 2645.00m,
            riskParams: riskParams
        );

        PrintResult(result);
        await Task.CompletedTask;
    }

    private static async Task TestScenario4(RiskManager riskManager)
    {
        System.Console.WriteLine("\nScenario 4: Silver (XAGUSD) Trade");
        System.Console.WriteLine("Account: $10,000 | Entry: 30.50 | Stop Loss: 30.00");

        var riskParams = new RiskParameters
        {
            AccountBalance = 10000m,
            InitialBalance = 10000m,
            PropFirmRule = "FTMO",
            RiskPercentPerTrade = 1.0m,
            TodayLoss = 0m,
            TotalLoss = 0m,
            LastResetDate = DateTime.UtcNow.Date.AddDays(-1)
        };

        var result = riskManager.CalculatePosition(
            symbol: "XAGUSD",
            broker: "ICMarkets",
            entryPrice: 30.50m,
            stopLoss: 30.00m,
            riskParams: riskParams
        );

        PrintResult(result);
        await Task.CompletedTask;
    }

    private static void PrintResult(PositionSizeResult result)
    {
        System.Console.WriteLine($"\n--- Result ---");
        System.Console.WriteLine($"Can Trade: {result.CanTrade}");
        System.Console.WriteLine($"Position Size: {result.PositionSize:F2} lots");
        System.Console.WriteLine($"Risk Amount: ${result.RiskAmount:F2}");
        System.Console.WriteLine($"Reason: {result.Reason}");

        if (result.Details != null)
        {
            var d = result.Details;
            System.Console.WriteLine($"\n--- Details ---");
            System.Console.WriteLine($"Entry: {d.EntryPrice:F2} | Stop Loss: {d.StopLoss:F2}");
            System.Console.WriteLine($"Risk in Pips: {d.PipsRisk:F2}");
            System.Console.WriteLine($"Per Lot Risk: ${d.PerLotRisk:F2}");
            System.Console.WriteLine($"Contract Size: {d.ContractSize} | Tick Size: {d.TickSize}");
            System.Console.WriteLine($"\nDaily Loss: ${d.DailyLoss:F2} / ${d.DailyLossLimit:F2} (Remaining: ${d.DailyRemainingRisk:F2})");
            System.Console.WriteLine($"Total Loss: ${d.TotalLoss:F2} / ${d.TotalLossLimit:F2} (Remaining: ${d.TotalRemainingRisk:F2})");
            System.Console.WriteLine($"Trading Day: {d.TradingDay:yyyy-MM-dd} (Server Time: {d.ServerTime:yyyy-MM-dd HH:mm:ss})");
        }

        System.Console.WriteLine();
    }
}
