using TradingBacktest.Console.Services;

namespace TradingBacktest.Console;

/// <summary>
/// 主程序入口
/// 职责：协调各服务完成回测流程
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        // 初始化服务
        var userInteraction = new UserInteractionService();
        var configService = new ConfigurationService();
        
        userInteraction.ShowWelcome();

        try
        {
            // 获取配置
            var config = configService.CreateDefaultConfig();
            var dataDirectory = configService.GetDataDirectory();
            
            // 运行回测
            var runner = new BacktestRunner(dataDirectory);
            var result = await runner.RunAsync(config);

            // 显示结果
            var printer = new ResultPrinter();
            printer.Print(result);

            // 保存结果（可选）
            if (userInteraction.AskToSaveResults())
            {
                var dbService = new DatabaseService(configService.GetCosmosConnectionString());
                await dbService.SaveResultAsync(result);
            }

            userInteraction.WaitForExit();
        }
        catch (Exception ex)
        {
            userInteraction.ShowError(ex);
            userInteraction.WaitForExit();
        }
    }
}
