using Trading.Backtest.ParameterOptimizer.Commands;

namespace Trading.Backtest.ParameterOptimizer;

/// <summary>
/// 程序入口
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        // 检查帮助命令
        if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h" || args[0] == "help"))
        {
            ShowHelp();
            return;
        }

        // 检查分析命令
        if (args.Length > 0 && args[0].ToLower() == "analyze")
        {
            AnalyzerCommand.Execute(args.Skip(1).ToArray());
            return;
        }

        // 默认运行优化器
        await OptimizerCommand.ExecuteAsync();
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Pin Bar 策略参数优化器");
        Console.WriteLine();
        Console.WriteLine("用法:");
        Console.WriteLine("  dotnet run                    运行参数优化（默认）");
        Console.WriteLine("  dotnet run -- analyze         分析最新的优化结果");
        Console.WriteLine("  dotnet run -- analyze <file>  分析指定的结果文件");
        Console.WriteLine("  dotnet run -- --help          显示此帮助信息");
        Console.WriteLine();
        Console.WriteLine("快捷命令:");
        Console.WriteLine("  ./analyze.sh  (Linux/Mac)    运行分析工具");
        Console.WriteLine("  analyze.bat   (Windows)      运行分析工具");
        Console.WriteLine();
        Console.WriteLine("示例:");
        Console.WriteLine("  dotnet run");
        Console.WriteLine("  dotnet run -- analyze");
        Console.WriteLine("  dotnet run -- analyze results/checkpoint_20260116_113522.json");
    }
}

