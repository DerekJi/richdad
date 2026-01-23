using Microsoft.Extensions.Options;
using Trading.AlertSystem.Data.Configuration;
using Trading.AlertSystem.Data.Services;
using Trading.AlertSystem.Service.Configuration;
using Trading.AlertSystem.Service.Repositories;
using Trading.AlertSystem.Service.Services;
using Trading.AlertSystem.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// 添加 User Secrets 支持（开发和测试环境）
builder.Configuration.AddUserSecrets<Program>(optional: true);

// 配置设置
builder.Services.Configure<TradeLockerSettings>(builder.Configuration.GetSection("TradeLocker"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<TradeLockerSettings>>().Value);

builder.Services.Configure<TelegramSettings>(builder.Configuration.GetSection("Telegram"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<TelegramSettings>>().Value);

builder.Services.Configure<MonitoringSettings>(builder.Configuration.GetSection("Monitoring"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<MonitoringSettings>>().Value);

// 注册数据库服务（如果配置了CosmosDB）
var cosmosConfig = builder.Configuration.GetSection("CosmosDb");
var connectionString = cosmosConfig["ConnectionString"];

if (!string.IsNullOrEmpty(connectionString))
{
    var cosmosSettings = new Trading.AlertSystem.Data.Configuration.CosmosDbSettings
    {
        ConnectionString = connectionString,
        DatabaseName = cosmosConfig["DatabaseName"] ?? "TradingSystem",
        AlertContainerName = cosmosConfig["AlertContainerName"] ?? "PriceAlerts"
    };

    builder.Services.AddSingleton(cosmosSettings);
    builder.Services.AddSingleton<Trading.AlertSystem.Data.Infrastructure.CosmosDbContext>();
    builder.Services.AddScoped<IPriceAlertRepository, PriceAlertRepository>();
}
else
{
    // 使用内存存储作为后备方案
    builder.Services.AddSingleton<IPriceAlertRepository, InMemoryPriceAlertRepository>();
}

// 注册数据层服务
var tradeLockerConfig = builder.Configuration.GetSection("TradeLocker");
if (!string.IsNullOrEmpty(tradeLockerConfig["Email"]) && !string.IsNullOrEmpty(tradeLockerConfig["Password"]))
{
    builder.Services.AddHttpClient<ITradeLockerService, TradeLockerService>();
}
else
{
    builder.Services.AddSingleton<ITradeLockerService, DemoTradeLockerService>();
}

var telegramConfig = builder.Configuration.GetSection("Telegram");
if (!string.IsNullOrEmpty(telegramConfig["BotToken"]))
{
    builder.Services.AddSingleton<ITelegramService, TelegramService>();
}
else
{
    builder.Services.AddSingleton<ITelegramService, DemoTelegramService>();
}

// 注册业务服务
builder.Services.AddSingleton<IPriceMonitorService, PriceMonitorService>();

// 添加后台服务（自动启动价格监控）
builder.Services.AddHostedService<PriceMonitorHostedService>();

// 添加TradeLocker启动测试服务
builder.Services.AddHostedService<TradeLockerStartupTestService>();

// 添加控制器
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// 配置CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 添加Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Trading Alert System API", Version = "v1" });
});

var app = builder.Build();

// 启用CORS
app.UseCors();

// 启用Swagger（开发和生产环境）
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Trading Alert System API V1");
    c.RoutePrefix = "swagger";
});

// 配置静态文件
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// 价格监控后台服务
/// </summary>
public class PriceMonitorHostedService : IHostedService
{
    private readonly IPriceMonitorService _monitorService;
    private readonly ILogger<PriceMonitorHostedService> _logger;

    public PriceMonitorHostedService(
        IPriceMonitorService monitorService,
        ILogger<PriceMonitorHostedService> logger)
    {
        _monitorService = monitorService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("启动价格监控后台服务");
        await _monitorService.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("停止价格监控后台服务");
        await _monitorService.StopAsync();
    }
}

/// <summary>
/// TradeLocker启动测试服务
/// </summary>
public class TradeLockerStartupTestService : IHostedService
{
    private readonly ITradeLockerService _tradeLockerService;
    private readonly ILogger<TradeLockerStartupTestService> _logger;

    public TradeLockerStartupTestService(
        ITradeLockerService tradeLockerService,
        ILogger<TradeLockerStartupTestService> logger)
    {
        _tradeLockerService = tradeLockerService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("=== 开始测试TradeLocker连接 ===");

        try
        {
            // 测试连接
            var connected = await _tradeLockerService.ConnectAsync();
            _logger.LogInformation("TradeLocker连接状态: {Connected}", connected);

            if (connected)
            {
                // 测试获取账户信息
                _logger.LogInformation("正在获取账户信息...");
                var accountInfo = await _tradeLockerService.GetAccountInfoAsync();

                if (accountInfo != null)
                {
                    _logger.LogInformation("✅ 账户信息获取成功:");
                    _logger.LogInformation("  账户ID: {AccountId}", accountInfo.AccountId);
                    _logger.LogInformation("  账户名称: {AccountName}", accountInfo.AccountName);
                    _logger.LogInformation("  余额: {Balance} {Currency}", accountInfo.Balance, accountInfo.Currency);
                    _logger.LogInformation("  净值: {Equity} {Currency}", accountInfo.Equity, accountInfo.Currency);
                    _logger.LogInformation("  已用保证金: {Margin} {Currency}", accountInfo.Margin, accountInfo.Currency);
                    _logger.LogInformation("  可用保证金: {FreeMargin} {Currency}", accountInfo.FreeMargin, accountInfo.Currency);
                }
                else
                {
                    _logger.LogWarning("❌ 未能获取账户信息");
                }
            }
            else
            {
                _logger.LogWarning("❌ TradeLocker连接失败");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 测试TradeLocker时发生异常");
        }

        _logger.LogInformation("=== TradeLocker连接测试完成 ===");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
