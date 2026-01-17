using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trading.Strategy.Analyzer.Analyzers;

namespace Trading.Strategy.Analyzer;

/// <summary>
/// 策略分析器主程序 - 提供多种策略分析工具
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        // 构建配置
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // 配置服务
        var services = new ServiceCollection();
        ConfigureServices(services, configuration);

        // 构建服务提供者
        var serviceProvider = services.BuildServiceProvider();

        // 解析命令参数
        var command = args.Length > 0 ? args[0].ToLower() : "performance2024";

        // 运行对应的分析器
        await RunAnalyzer(command, serviceProvider);
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 注册配置
        services.AddSingleton(configuration);

        // 注册分析器
        services.AddTransient<Performance2024Analyzer>();

        // 未来可以添加更多分析器
        // services.AddTransient<PerformanceComparisonAnalyzer>();
        // services.AddTransient<MarketConditionAnalyzer>();
    }

    private static async Task RunAnalyzer(string command, ServiceProvider serviceProvider)
    {
        IAnalyzer? analyzer = command switch
        {
            "performance2024" or "2024" => serviceProvider.GetRequiredService<Performance2024Analyzer>(),
            // 未来添加更多分析器命令
            // "compare" => serviceProvider.GetRequiredService<PerformanceComparisonAnalyzer>(),
            // "market" => serviceProvider.GetRequiredService<MarketConditionAnalyzer>(),
            _ => null
        };

        if (analyzer == null)
        {
            Console.WriteLine($"未知命令: {command}");
            Console.WriteLine("\n可用命令:");
            Console.WriteLine("  performance2024 (或 2024) - 分析2024年策略表现");
            // Console.WriteLine("  compare - 比较不同策略表现");
            // Console.WriteLine("  market - 分析市场环境");
            return;
        }

        await analyzer.RunAsync();
    }
}
