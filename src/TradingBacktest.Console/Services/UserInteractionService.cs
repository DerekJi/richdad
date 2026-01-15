namespace TradingBacktest.Console.Services;

/// <summary>
/// 用户交互服务
/// </summary>
public class UserInteractionService
{
    /// <summary>
    /// 询问用户是否保存结果
    /// </summary>
    public bool AskToSaveResults()
    {
        System.Console.Write("\n是否保存回测结果到Cosmos DB? (y/n): ");
        var input = System.Console.ReadLine();
        return input?.ToLower() == "y";
    }

    /// <summary>
    /// 等待用户按键
    /// </summary>
    public void WaitForExit()
    {
        System.Console.WriteLine("\n回测完成！按任意键退出...");
        System.Console.ReadKey();
    }

    /// <summary>
    /// 显示欢迎信息
    /// </summary>
    public void ShowWelcome()
    {
        System.Console.OutputEncoding = System.Text.Encoding.UTF8;
        System.Console.WriteLine("=== 交易策略回测系统 ===\n");
    }

    /// <summary>
    /// 显示错误信息
    /// </summary>
    public void ShowError(Exception ex)
    {
        System.Console.WriteLine($"\n错误: {ex.Message}");
        System.Console.WriteLine(ex.StackTrace);
    }
}
