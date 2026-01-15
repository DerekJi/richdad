using System.Text.Json;
using Trading.Data.Configuration;
using Trading.Data.Infrastructure;
using Trading.Data.Models;
using Trading.Data.Repositories;

namespace Trading.Backtest.Console;

/// <summary>
/// Cosmos DB测试命令 - 用于调试保存问题
/// </summary>
public class CosmosTestCommand
{
    public static async Task RunAsync(string[] args)
    {
        if (args.Length < 2)
        {
            System.Console.WriteLine("用法: dotnet run cosmos-test <命令>");
            System.Console.WriteLine("命令:");
            System.Console.WriteLine("  generate       - 生成测试JSON文件");
            System.Console.WriteLine("  save <file>    - 尝试保存指定的JSON文件到Cosmos DB");
            System.Console.WriteLine("  test-minimal   - 测试保存最小化的BacktestResult");
            return;
        }

        var command = args[1].ToLower();

        switch (command)
        {
            case "generate":
                await GenerateTestJson();
                break;
            case "save":
                if (args.Length < 3)
                {
                    System.Console.WriteLine("错误: 需要指定JSON文件路径");
                    return;
                }
                await TestSaveFromJson(args[2]);
                break;
            case "test-minimal":
                await TestMinimalSave();
                break;
            default:
                System.Console.WriteLine($"未知命令: {command}");
                break;
        }
    }

    private static async Task GenerateTestJson()
    {
        System.Console.WriteLine("正在运行回测生成完整数据...");
        
        // 这里需要实际运行一次回测来获取真实数据
        // 暂时生成一个简化的测试结构
        var testResult = new BacktestResult
        {
            Id = Guid.NewGuid().ToString(),
            BacktestTime = DateTime.UtcNow,
            StartTime = new DateTime(2021, 10, 20),
            EndTime = new DateTime(2024, 1, 12),
            Config = new StrategyConfig
            {
                StrategyName = "PinBar-XAUUSD-Test",
                Symbol = "XAUUSD",
                ContractSize = 100,
                BaseEma = 200,
                AtrPeriod = 14
            },
            Trades = new List<Trade>()
        };

        // 添加一些测试交易
        for (int i = 0; i < 60; i++)
        {
            testResult.Trades.Add(new Trade
            {
                Id = Guid.NewGuid().ToString(),
                Direction = i % 2 == 0 ? TradeDirection.Long : TradeDirection.Short,
                OpenTime = DateTime.UtcNow.AddHours(-100 + i),
                OpenPrice = 1800 + i * 0.5m,
                StopLoss = 1790m,
                TakeProfit = 1810m,
                CloseTime = DateTime.UtcNow.AddHours(-99 + i),
                ClosePrice = 1805m,
                CloseReason = TradeCloseReason.TakeProfit,
                ProfitLoss = 500m,
                ReturnRate = 0.5m
            });
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new DecimalJsonConverter(8),
                new TimeSpanConverter()
            }
        };

        var json = JsonSerializer.Serialize(testResult, options);
        var filePath = "cosmos_test_data.json";
        File.WriteAllText(filePath, json);
        
        System.Console.WriteLine($"✓ 测试数据已生成: {Path.GetFullPath(filePath)}");
        System.Console.WriteLine($"  包含 {testResult.Trades.Count} 笔交易");
    }

    private static async Task TestSaveFromJson(string filePath)
    {
        if (!File.Exists(filePath))
        {
            System.Console.WriteLine($"错误: 文件不存在: {filePath}");
            return;
        }

        System.Console.WriteLine($"正在读取JSON文件: {filePath}");
        var json = File.ReadAllText(filePath);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new DecimalJsonConverter(8),
                new TimeSpanConverter()
            }
        };

        var result = JsonSerializer.Deserialize<BacktestResult>(json, options);
        if (result == null)
        {
            System.Console.WriteLine("错误: 无法反序列化JSON");
            return;
        }

        System.Console.WriteLine($"✓ JSON读取成功");
        System.Console.WriteLine($"  策略: {result.Config.StrategyName}");
        System.Console.WriteLine($"  交易数: {result.Trades.Count}");

        // 初始化Cosmos DB
        System.Console.WriteLine("\n正在连接Cosmos DB...");
        var cosmosSettings = new CosmosDbSettings
        {
            ConnectionString = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
            DatabaseName = "TradingBacktest",
            BacktestContainerName = "BacktestResults",
            ConfigContainerName = "StrategyConfigs"
        };
        var dbContext = new CosmosDbContext(cosmosSettings);
        await dbContext.InitializeAsync();

        var repository = new CosmosBacktestRepository(dbContext);

        // 尝试保存
        System.Console.WriteLine("\n正在保存到Cosmos DB...");
        try
        {
            var savedId = await repository.SaveBacktestResultAsync(result);
            System.Console.WriteLine($"✓ 保存成功! ID: {savedId}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"✗ 保存失败: {ex.Message}");
            System.Console.WriteLine($"\n完整错误:\n{ex}");
        }
    }

    private static async Task TestMinimalSave()
    {
        System.Console.WriteLine("测试保存最小化BacktestResult...\n");

        var result = new BacktestResult
        {
            Id = Guid.NewGuid().ToString(),
            BacktestTime = DateTime.UtcNow,
            StartTime = DateTime.UtcNow.AddDays(-30),
            EndTime = DateTime.UtcNow,
            Config = new StrategyConfig
            {
                StrategyName = "MinimalTest",
                Symbol = "XAUUSD",
                ContractSize = 100
            },
            Trades = new List<Trade>
            {
                new Trade
                {
                    Id = Guid.NewGuid().ToString(),
                    Direction = TradeDirection.Long,
                    OpenTime = DateTime.UtcNow.AddHours(-1),
                    OpenPrice = 1800.00m,
                    StopLoss = 1795.00m,
                    TakeProfit = 1810.00m,
                    CloseTime = DateTime.UtcNow,
                    ClosePrice = 1810.00m,
                    CloseReason = TradeCloseReason.TakeProfit,
                    ProfitLoss = 1000.00m,
                    ReturnRate = 1.00m
                }
            },
            OverallMetrics = new PerformanceMetrics
            {
                TotalTrades = 1,
                WinningTrades = 1,
                LosingTrades = 0,
                TotalProfit = 1000.00m,
                TotalReturnRate = 1.00m
            }
        };

        System.Console.WriteLine("测试数据:");
        System.Console.WriteLine($"  ID: {result.Id}");
        System.Console.WriteLine($"  交易数: {result.Trades.Count}");

        // 初始化Cosmos DB
        System.Console.WriteLine("\n正在连接Cosmos DB...");
        var cosmosSettings = new CosmosDbSettings
        {
            ConnectionString = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
            DatabaseName = "TradingBacktest",
            BacktestContainerName = "BacktestResults",
            ConfigContainerName = "StrategyConfigs"
        };
        var dbContext = new CosmosDbContext(cosmosSettings);
        await dbContext.InitializeAsync();

        var repository = new CosmosBacktestRepository(dbContext);

        // 尝试保存
        System.Console.WriteLine("\n正在保存到Cosmos DB...");
        try
        {
            var savedId = await repository.SaveBacktestResultAsync(result);
            System.Console.WriteLine($"✓ 保存成功! ID: {savedId}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"✗ 保存失败: {ex.Message}");
            System.Console.WriteLine($"\n完整错误:\n{ex}");
        }
    }
}
