using System.Text.Json;
using Trading.Data.Models;
using Trading.Data.Infrastructure;

var result = new BacktestResult
{
    Config = new StrategyConfig
    {
        StrategyName = "Test",
        Symbol = "XAUUSD",
        Leverage = 30,
        InitialCapital = 100000
    },
    OverallMetrics = new PerformanceMetrics
    {
        TotalTrades = 1,
        AverageHoldingTime = TimeSpan.FromHours(5)
    }
};

var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true,
    Converters = { new TimeSpanConverter() }
};

var json = JsonSerializer.Serialize(result, options);
Console.WriteLine(json);
