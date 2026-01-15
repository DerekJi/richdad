using TradingBacktest.Data.Models;

namespace TradingBacktest.Console.Services;

/// <summary>
/// 配置服务
/// </summary>
public class ConfigurationService
{
    /// <summary>
    /// 创建默认配置
    /// </summary>
    public StrategyConfig CreateDefaultConfig()
    {
        var config = StrategyConfig.CreateXauDefault();
        config.StrategyName = "PinBar-XAUUSD-v1";
        return config;
    }

    /// <summary>
    /// 获取数据目录路径
    /// </summary>
    public string GetDataDirectory()
    {
        // 设置数据目录 - 使用绝对路径或相对路径
        var dataDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "data"));
        if (!Directory.Exists(dataDirectory))
        {
            // 如果找不到，尝试从当前工作目录查找
            dataDirectory = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "data"));
        }
        return dataDirectory;
    }

    /// <summary>
    /// 获取Cosmos DB连接字符串
    /// </summary>
    public string GetCosmosConnectionString()
    {
        return "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
    }
}
