using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trading.Backtest.Console.Services;
using Trading.Backtest.Services;
using Trading.Data.Configuration;
using Trading.Data.Infrastructure;
using Trading.Data.Interfaces;
using Trading.Data.Repositories;

namespace Trading.Backtest.Console;

/// <summary>
/// 主程序入口
/// 职责：配置依赖注入容器，协调各服务完成回测流程
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        // 检查是否是cosmos-test命令
        if (args.Length > 0 && args[0].ToLower() == "cosmos-test")
        {
            await CosmosTestCommand.RunAsync(args);
            return;
        }

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

        // 初始化 Cosmos DB
        var dbContext = serviceProvider.GetRequiredService<CosmosDbContext>();
        await dbContext.InitializeAsync();

        // 运行应用
        var app = serviceProvider.GetRequiredService<Application>();
        await app.RunAsync(args);
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 绑定配置
        var appSettings = new AppSettings();
        configuration.Bind(appSettings);
        
        if (appSettings.Strategies.Count == 0)
        {
            throw new InvalidOperationException("无法加载配置文件或策略配置为空");
        }
        
        services.AddSingleton(appSettings);
        services.AddSingleton(appSettings.CosmosDb);

        // 注册Data层服务
        services.AddSingleton<CosmosDbContext>();
        services.AddScoped<IBacktestRepository, CosmosBacktestRepository>();
        services.AddScoped<IStrategyConfigRepository, CosmosStrategyConfigRepository>();

        // 注册Console层服务
        services.AddScoped<UserInteractionService>();
        services.AddScoped<ConfigurationService>();
        services.AddScoped<BacktestRunner>();
        services.AddScoped<ResultPrinter>();
        services.AddScoped<DatabaseService>();
        services.AddScoped<FileReportService>();
        services.AddScoped<Application>();
    }
}

/// <summary>
/// 应用主类
/// </summary>
class Application
{
    private readonly UserInteractionService _userInteraction;
    private readonly ConfigurationService _configService;
    private readonly BacktestRunner _runner;
    private readonly ResultPrinter _printer;
    private readonly DatabaseService _dbService;
    private readonly FileReportService _fileReportService;

    public Application(
        UserInteractionService userInteraction,
        ConfigurationService configService,
        BacktestRunner runner,
        ResultPrinter printer,
        DatabaseService dbService,
        FileReportService fileReportService)
    {
        _userInteraction = userInteraction;
        _configService = configService;
        _runner = runner;
        _printer = printer;
        _dbService = dbService;
        _fileReportService = fileReportService;
    }

    public async Task RunAsync(string[] args)
    {
        _userInteraction.ShowWelcome();

        try
        {
            // 从命令行参数或用户交互获取策略名称
            var strategyName = GetStrategyName(args, _configService.GetAvailableStrategies());
            var config = _configService.GetStrategyConfig(strategyName);
            var accountSettings = _configService.GetAccountSettings();
            var dataDirectory = _configService.GetDataDirectory();
            
            // 运行回测
            var result = await _runner.RunAsync(config, accountSettings, dataDirectory);

            // 显示结果
            _printer.Print(result, (decimal)accountSettings.InitialCapital, (decimal)accountSettings.Leverage);

            // 运行诊断分析
            var diagnosticService = new StrategyDiagnosticService(config);
            var diagnosticResult = diagnosticService.Analyze(_runner.LoadedCandles);
            diagnosticService.PrintDiagnostic(diagnosticResult);
            
            // 生成诊断文本用于保存到文件
            var diagnosticText = diagnosticService.GenerateDiagnosticText(diagnosticResult);

            // 保存报告到文件
            try
            {
                var reportPath = _fileReportService.SaveReport(
                    result, 
                    (decimal)accountSettings.InitialCapital, 
                    (decimal)accountSettings.Leverage,
                    diagnosticText);
                
                var chartPath = reportPath.Replace(".txt", ".png");
                System.Console.WriteLine($"\n✓ 报告已保存到文件: {reportPath}");
                System.Console.WriteLine($"✓ 收益曲线图已保存: {chartPath}");
            }
            catch (Exception fileEx)
            {
                System.Console.WriteLine($"\n✗ 保存报告文件失败: {fileEx.Message}");
            }

            // 自动保存结果到Cosmos DB
            try
            {
                await _dbService.SaveResultAsync(result);
            }
            catch (Exception dbEx)
            {
                _userInteraction.ShowError(dbEx);
                System.Console.WriteLine("\n警告: Cosmos DB保存失败，但回测结果已成功显示。");
            }
        }
        catch (Exception ex)
        {
            _userInteraction.ShowError(ex);
        }
    }

    private string GetStrategyName(string[] args, List<string> availableStrategies)
    {
        const string defaultStrategy = "PinBar-XAUUSD-v2";

        // 如果有命令行参数，使用第一个参数作为策略名称
        if (args.Length > 0)
        {
            var strategyArg = args[0];
            if (availableStrategies.Contains(strategyArg))
            {
                System.Console.WriteLine($"使用命令行参数指定的策略: {strategyArg}\n");
                return strategyArg;
            }
            else
            {
                System.Console.WriteLine($"警告: 策略 '{strategyArg}' 不存在，可用策略: {string.Join(", ", availableStrategies)}");
                System.Console.WriteLine($"使用默认策略: {defaultStrategy}\n");
                return defaultStrategy;
            }
        }

        // 没有命令行参数，让用户交互选择（默认为PinBar-XAUUSD-v1）
        return _userInteraction.SelectStrategy(availableStrategies, defaultStrategy);
    }
}
