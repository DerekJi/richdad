namespace Trading.Backtest.Console.Services;

/// <summary>
/// 用户交互服务
/// 职责：处理控制台输入输出
/// </summary>
public class UserInteractionService
{
    /// <summary>
    /// 让用户选择策略
    /// </summary>
    /// <param name="availableStrategies">可用策略列表</param>
    /// <param name="defaultStrategy">默认策略名称（可选）</param>
    public string SelectStrategy(List<string> availableStrategies, string? defaultStrategy = null)
    {
        var actualDefault = defaultStrategy ?? availableStrategies[0];

        System.Console.WriteLine("可用的策略：");
        for (int i = 0; i < availableStrategies.Count; i++)
        {
            var marker = availableStrategies[i] == actualDefault ? " [默认]" : "";
            System.Console.WriteLine($"{i + 1}. {availableStrategies[i]}{marker}");
        }

        System.Console.Write($"\n请选择策略 (1-{availableStrategies.Count})，或直接回车使用默认: ");
        var input = System.Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            System.Console.WriteLine($"使用默认策略: {actualDefault}\n");
            return actualDefault;
        }

        if (int.TryParse(input, out int index) && index >= 1 && index <= availableStrategies.Count)
        {
            var selected = availableStrategies[index - 1];
            System.Console.WriteLine($"已选择: {selected}\n");
            return selected;
        }

        System.Console.WriteLine($"无效的选择，使用默认策略: {actualDefault}\n");
        return actualDefault;
    }

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
