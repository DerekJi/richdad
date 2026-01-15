using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Binder;
using Microsoft.Extensions.DependencyInjection;
using TradingBacktest.Console.Services;
using TradingBacktest.Data.Configuration;
using TradingBacktest.Data.Infrastructure;
using TradingBacktest.Data.Interfaces;
using TradingBacktest.Data.Repositories;

namespace TradingBacktest.Console;

/// <summary>
/// 主程序入口
/// 职责：配置依赖注入容器，协调各服务完成回测流程
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

        // 初始化 Cosmos DB
        var dbContext = serviceProvider.GetRequiredService<CosmosDbContext>();
        await dbContext.InitializeAsync();

        // 运行应用
        var app = serviceProvider.GetRequiredService<Application>();
        await app.RunAsync();
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

    public Application(
        UserInteractionService userInteraction,
        ConfigurationService configService,
        BacktestRunner runner,
        ResultPrinter printer,
        DatabaseService dbService)
    {
        _userInteraction = userInteraction;
        _configService = configService;
        _runner = runner;
        _printer = printer;
        _dbService = dbService;
    }

    public async Task RunAsync()
    {
        _userInteraction.ShowWelcome();

        try
        {
            // 让用户选择策略
            var strategyName = _userInteraction.SelectStrategy(_configService.GetAvailableStrategies());
            var config = _configService.GetStrategyConfig(strategyName);
            var dataDirectory = _configService.GetDataDirectory();
            
            // 运行回测
            var result = await _runner.RunAsync(config, dataDirectory);

            // 显示结果
            _printer.Print(result);

            // 保存结果（可选）
            if (_userInteraction.AskToSaveResults())
            {
                await _dbService.SaveResultAsync(result);
            }

            _userInteraction.WaitForExit();
        }
        catch (Exception ex)
        {
            _userInteraction.ShowError(ex);
            _userInteraction.WaitForExit();
        }
    }
}
